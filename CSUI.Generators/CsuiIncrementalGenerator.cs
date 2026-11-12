#nullable enable
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace CSUI.Generators
{
    [Generator(LanguageNames.CSharp)]
    public sealed class CsuiIncrementalGenerator : IIncrementalGenerator
    {
        private const string ObservableObjectAttrName = "System.Windows.Markup.ObservableObjectAttribute";
        private const string ObservablePropertyAttrName = "System.Windows.Markup.ObservablePropertyAttribute";
        private const string AlsoNotifyChangeForAttrName = "System.Windows.Markup.AlsoNotifyChangeForAttribute";
        private const string RelayCommandAttrName = "System.Windows.Markup.RelayCommandAttribute";
        private const string BindableAttrName = "System.Windows.Markup.BindableAttribute";
        private const string AotBindableAttrName = "System.Windows.Markup.AotBindableAttribute";
        private const string ComponentModelBindableAttrName = "System.ComponentModel.BindableAttribute";
        private const string XamlTypeAttrName = "System.Windows.Markup.XamlTypeAttribute";

        public void Initialize(IncrementalGeneratorInitializationContext context)
        {
            // 1. Collect candidate type declarations with attributes
            IncrementalValuesProvider<TypeModel?> candidateTypes = context.SyntaxProvider
                .CreateSyntaxProvider(
                    predicate: static (node, _) => node is TypeDeclarationSyntax tds && tds.AttributeLists.Count > 0,
                    transform: static (ctx, ct) => ExtractTypeModel(ctx, ct))
                .Where(static m => m is not null);

            // 2. Emit partial classes for [ObservableObject] types and assembly-wide [ModuleInitializer] for [ObservableObject], [Bindable], and [XamlType]
            IncrementalValueProvider<ImmutableArray<TypeModel?>> allTypes = candidateTypes.Collect();

            context.RegisterSourceOutput(allTypes, static (spc, models) =>
            {
                if (models.IsDefaultOrEmpty)
                {
                    return;
                }

                // Deduplicate by fully-qualified metadata name (partial classes may appear across multiple syntax trees)
                var uniqueModels = new Dictionary<string, TypeModel>(StringComparer.Ordinal);
                foreach (TypeModel? model in models)
                {
                    if (model is null)
                    {
                        continue;
                    }
                    if (!uniqueModels.ContainsKey(model.FullTypeName))
                    {
                        uniqueModels[model.FullTypeName] = model;
                    }
                }

                foreach (TypeModel model in uniqueModels.Values)
                {
                    if (model.IsObservableObject && model.IsPartial)
                    {
                        string partialSource = GenerateObservableObjectPartial(model);
                        string safeHint = model.FullTypeName
                            .Replace("global::", "")
                            .Replace('.', '_')
                            .Replace('<', '_')
                            .Replace('>', '_');
                        spc.AddSource($"{safeHint}.ObservableObject.g.cs", SourceText.From(partialSource, Encoding.UTF8));
                    }
                }

                string? moduleInitSource = GenerateModuleInitializer(uniqueModels.Values);
                if (!string.IsNullOrEmpty(moduleInitSource))
                {
                    spc.AddSource("CsuiGeneratedModuleInitializer.g.cs", SourceText.From(moduleInitSource!, Encoding.UTF8));
                }
            });

            // 3. Process .xaml AdditionalFiles with x:Class="..."
            IncrementalValuesProvider<AdditionalText> xamlFiles = context.AdditionalTextsProvider
                .Where(static file => file.Path.EndsWith(".xaml", StringComparison.OrdinalIgnoreCase));

            context.RegisterSourceOutput(xamlFiles, static (spc, xamlFile) =>
            {
                SourceText? text = xamlFile.GetText();
                if (text is null)
                {
                    return;
                }

                string rawXaml = text.ToString();
                if (string.IsNullOrWhiteSpace(rawXaml))
                {
                    return;
                }

                string? generated = TryGenerateXamlPartialClass(xamlFile.Path, rawXaml);
                if (!string.IsNullOrEmpty(generated))
                {
                    string fileName = Path.GetFileNameWithoutExtension(xamlFile.Path);
                    spc.AddSource($"{fileName}.xaml.g.cs", SourceText.From(generated!, Encoding.UTF8));
                }
            });
        }

        private static TypeModel? ExtractTypeModel(GeneratorSyntaxContext ctx, System.Threading.CancellationToken ct)
        {
            if (ctx.Node is not TypeDeclarationSyntax typeDecl)
            {
                return null;
            }

            if (ctx.SemanticModel.GetDeclaredSymbol(typeDecl, ct) is not INamedTypeSymbol symbol)
            {
                return null;
            }

            if (symbol.IsGenericType || symbol.IsAbstract)
            {
                // Generic open types cannot be registered directly in non-generic module initializer
            }

            bool isObservableObject = HasAttribute(symbol, ObservableObjectAttrName);
            bool isBindable = HasAttribute(symbol, BindableAttrName) ||
                              HasAttribute(symbol, AotBindableAttrName) ||
                              HasAttribute(symbol, ComponentModelBindableAttrName);
            bool isXamlType = HasAttribute(symbol, XamlTypeAttrName);

            if (!isObservableObject && !isBindable && !isXamlType)
            {
                return null;
            }

            bool isPartial = typeDecl.Modifiers.Any(static m => m.IsKind(SyntaxKind.PartialKeyword));
            bool alreadyImplementsInpc = symbol.AllInterfaces.Any(
                static i => i.ToDisplayString() == "System.ComponentModel.INotifyPropertyChanged");

            var observableFields = new List<ObservableFieldModel>();
            var relayCommands = new List<RelayCommandModel>();
            var publicProperties = new List<BindablePropertyModel>();
            var indexers = new List<BindableIndexerModel>();
            var declaredClrPropertiesForXaml = new List<BindablePropertyModel>();
            var declaredDependencyPropertiesForXaml = new List<(string ShortName, string FieldName)>();

            var seenPropNames = new HashSet<string>(StringComparer.Ordinal);

            foreach (ISymbol member in symbol.GetMembers())
            {
                if (member.IsStatic)
                {
                    continue;
                }

                if (isObservableObject && member is IFieldSymbol field && HasAttribute(field, ObservablePropertyAttrName))
                {
                    string propName = ConvertFieldNameToPropertyName(field.Name);
                    string fieldType = field.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
                    var notifyAlso = new List<string>();

                    foreach (AttributeData attr in field.GetAttributes())
                    {
                        if (attr.AttributeClass?.ToDisplayString() == AlsoNotifyChangeForAttrName)
                        {
                            foreach (TypedConstant arg in attr.ConstructorArguments)
                            {
                                if (arg.Kind == TypedConstantKind.Array)
                                {
                                    foreach (TypedConstant elem in arg.Values)
                                    {
                                        if (elem.Value is string s && !string.IsNullOrEmpty(s))
                                        {
                                            notifyAlso.Add(s);
                                        }
                                    }
                                }
                                else if (arg.Value is string s && !string.IsNullOrEmpty(s))
                                {
                                    notifyAlso.Add(s);
                                }
                            }
                        }
                    }

                    observableFields.Add(new ObservableFieldModel(field.Name, propName, fieldType, notifyAlso));
                    if (seenPropNames.Add(propName))
                    {
                        publicProperties.Add(new BindablePropertyModel(propName, fieldType, CanRead: true, CanWrite: true));
                    }
                }
                else if (isObservableObject && member is IMethodSymbol method && HasAttribute(method, RelayCommandAttrName))
                {
                    if (method.Parameters.Length <= 1)
                    {
                        string? paramType = method.Parameters.Length == 1
                            ? method.Parameters[0].Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)
                            : null;

                        string? canExecuteMember = null;
                        bool canExecuteIsProperty = false;

                        foreach (AttributeData attr in method.GetAttributes())
                        {
                            if (attr.AttributeClass?.ToDisplayString() == RelayCommandAttrName)
                            {
                                foreach (KeyValuePair<string, TypedConstant> named in attr.NamedArguments)
                                {
                                    if (named.Key == "CanExecute" && named.Value.Value is string ce && !string.IsNullOrEmpty(ce))
                                    {
                                        canExecuteMember = ce;
                                        canExecuteIsProperty = symbol.GetMembers(ce).Any(static m => m is IPropertySymbol);
                                    }
                                }
                            }
                        }

                        string baseName = method.Name.EndsWith("Command", StringComparison.Ordinal)
                            ? method.Name.Substring(0, method.Name.Length - "Command".Length)
                            : method.Name;
                        string cmdPropName = baseName + "Command";
                        string cmdFieldName = "_" + char.ToLowerInvariant(baseName[0]) + baseName.Substring(1) + "Command";
                        string cmdType = paramType is null
                            ? "global::System.Windows.Markup.RelayCommand"
                            : $"global::System.Windows.Markup.RelayCommand<{paramType}>";

                        relayCommands.Add(new RelayCommandModel(
                            method.Name,
                            cmdPropName,
                            cmdFieldName,
                            paramType,
                            canExecuteMember,
                            canExecuteIsProperty));

                        if (seenPropNames.Add(cmdPropName))
                        {
                            publicProperties.Add(new BindablePropertyModel(cmdPropName, cmdType, CanRead: true, CanWrite: false));
                        }
                    }
                }
            }

            // Collect public instance properties & indexers across the type hierarchy for [ObservableObject] / [Bindable]
            if (isObservableObject || isBindable)
            {
                INamedTypeSymbol? current = symbol;
                while (current != null &&
                       current.SpecialType != SpecialType.System_Object &&
                       current.ToDisplayString() != "System.Windows.DependencyObject" &&
                       current.ToDisplayString() != "System.Windows.Threading.DispatcherObject")
                {
                    foreach (ISymbol member in current.GetMembers())
                    {
                        if (member.IsStatic || member.DeclaredAccessibility != Accessibility.Public)
                        {
                            continue;
                        }

                        if (member is IPropertySymbol prop)
                        {
                            if (prop.IsIndexer)
                            {
                                if (prop.Parameters.Length == 1)
                                {
                                    string idxType = prop.Parameters[0].Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
                                    string valType = prop.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
                                    bool canRead = prop.GetMethod != null && prop.GetMethod.DeclaredAccessibility == Accessibility.Public;
                                    bool canWrite = prop.SetMethod != null && prop.SetMethod.DeclaredAccessibility == Accessibility.Public && !prop.SetMethod.IsInitOnly;
                                    if (canRead)
                                    {
                                        indexers.Add(new BindableIndexerModel(idxType, valType, canWrite));
                                    }
                                }
                            }
                            else
                            {
                                bool canRead = prop.GetMethod != null && prop.GetMethod.DeclaredAccessibility == Accessibility.Public;
                                bool canWrite = prop.SetMethod != null && prop.SetMethod.DeclaredAccessibility == Accessibility.Public && !prop.SetMethod.IsInitOnly;
                                if (canRead && seenPropNames.Add(prop.Name))
                                {
                                    string propType = prop.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
                                    publicProperties.Add(new BindablePropertyModel(prop.Name, propType, canRead, canWrite));
                                }
                            }
                        }
                    }
                    current = current.BaseType;
                }
            }

            // For [XamlType], collect declared public read/write CLR properties that do not have a corresponding FooProperty DependencyProperty
            if (isXamlType)
            {
                var dpNames = new HashSet<string>(StringComparer.Ordinal);
                INamedTypeSymbol? c = symbol;
                while (c != null && c.SpecialType != SpecialType.System_Object)
                {
                    foreach (ISymbol m in c.GetMembers())
                    {
                        if (m.IsStatic && m.Name.EndsWith("Property", StringComparison.Ordinal))
                        {
                            dpNames.Add(m.Name.Substring(0, m.Name.Length - "Property".Length));
                        }
                    }
                    c = c.BaseType;
                }

                foreach (ISymbol member in symbol.GetMembers())
                {
                    if (member.IsStatic &&
                        member.DeclaredAccessibility == Accessibility.Public &&
                        member is IFieldSymbol dpField &&
                        dpField.Name.EndsWith("Property", StringComparison.Ordinal) &&
                        dpField.Type.ToDisplayString() == "System.Windows.DependencyProperty")
                    {
                        string shortName = dpField.Name.Substring(0, dpField.Name.Length - "Property".Length);
                        if (!string.IsNullOrEmpty(shortName))
                        {
                            declaredDependencyPropertiesForXaml.Add((shortName, dpField.Name));
                        }
                    }
                    else if (!member.IsStatic && member.DeclaredAccessibility == Accessibility.Public && member is IPropertySymbol prop && !prop.IsIndexer)
                    {
                        bool canRead = prop.GetMethod != null && prop.GetMethod.DeclaredAccessibility == Accessibility.Public;
                        bool canWrite = prop.SetMethod != null && prop.SetMethod.DeclaredAccessibility == Accessibility.Public && !prop.SetMethod.IsInitOnly;
                        if (canRead && canWrite && !dpNames.Contains(prop.Name))
                        {
                            string propType = prop.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
                            declaredClrPropertiesForXaml.Add(new BindablePropertyModel(prop.Name, propType, true, true));
                        }
                    }
                }
            }

            bool hasParameterlessCtor = !symbol.IsAbstract &&
                (symbol.InstanceConstructors.Length == 0 ||
                 symbol.InstanceConstructors.Any(static ctor =>
                     ctor.Parameters.Length == 0 &&
                     (ctor.DeclaredAccessibility == Accessibility.Public || ctor.DeclaredAccessibility == Accessibility.Internal)));

            string ns = symbol.ContainingNamespace.IsGlobalNamespace
                ? string.Empty
                : symbol.ContainingNamespace.ToDisplayString();

            string accessibility = symbol.DeclaredAccessibility switch
            {
                Accessibility.Public => "public",
                Accessibility.Internal => "internal",
                _ => "internal"
            };

            return new TypeModel(
                Namespace: ns,
                TypeName: symbol.Name,
                FullTypeName: symbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
                AccessibilityKeyword: accessibility,
                IsStruct: symbol.TypeKind == TypeKind.Struct,
                IsSealed: symbol.IsSealed,
                IsPartial: isPartial,
                IsAbstract: symbol.IsAbstract,
                IsGeneric: symbol.IsGenericType,
                HasParameterlessCtor: hasParameterlessCtor,
                IsObservableObject: isObservableObject,
                IsBindable: isBindable,
                IsXamlType: isXamlType,
                AlreadyImplementsInpc: alreadyImplementsInpc,
                ObservableFields: observableFields,
                RelayCommands: relayCommands,
                PublicProperties: publicProperties,
                Indexers: indexers,
                DeclaredXamlClrProperties: declaredClrPropertiesForXaml,
                DeclaredXamlDependencyProperties: declaredDependencyPropertiesForXaml);
        }

        private static bool HasAttribute(ISymbol symbol, string fullAttributeName)
        {
            foreach (AttributeData attr in symbol.GetAttributes())
            {
                if (attr.AttributeClass?.ToDisplayString() == fullAttributeName)
                {
                    return true;
                }
            }
            return false;
        }

        private static string ConvertFieldNameToPropertyName(string fieldName)
        {
            string trimmed = fieldName;
            if (trimmed.StartsWith("m_", StringComparison.Ordinal) && trimmed.Length > 2)
            {
                trimmed = trimmed.Substring(2);
            }
            else if (trimmed.StartsWith("_", StringComparison.Ordinal) && trimmed.Length > 1)
            {
                trimmed = trimmed.Substring(1);
            }

            if (trimmed.Length == 0)
            {
                return "GeneratedProperty";
            }

            return char.ToUpperInvariant(trimmed[0]) + trimmed.Substring(1);
        }

        private static string GenerateObservableObjectPartial(TypeModel model)
        {
            var sb = new StringBuilder();
            sb.AppendLine("// <auto-generated />");
            sb.AppendLine("#nullable enable");
            sb.AppendLine();

            bool hasNamespace = !string.IsNullOrEmpty(model.Namespace);
            if (hasNamespace)
            {
                sb.Append("namespace ").Append(model.Namespace).AppendLine();
                sb.AppendLine("{");
            }

            string indent = hasNamespace ? "    " : "";
            string memberIndent = indent + "    ";

            string baseList = model.AlreadyImplementsInpc
                ? string.Empty
                : " : global::System.ComponentModel.INotifyPropertyChanged";

            sb.Append(indent)
              .Append(model.AccessibilityKeyword)
              .Append(" partial ")
              .Append(model.IsStruct ? "struct " : "class ")
              .Append(model.TypeName)
              .AppendLine(baseList);
            sb.Append(indent).AppendLine("{");

            if (!model.AlreadyImplementsInpc)
            {
                string helperAccess = (model.IsSealed || model.IsStruct) ? "private" : "protected";
                sb.Append(memberIndent).AppendLine("public event global::System.ComponentModel.PropertyChangedEventHandler? PropertyChanged;");
                sb.AppendLine();
                sb.Append(memberIndent).Append(helperAccess).AppendLine(" void OnPropertyChanged([global::System.Runtime.CompilerServices.CallerMemberName] string? propertyName = null)");
                sb.Append(memberIndent).AppendLine("{");
                sb.Append(memberIndent).AppendLine("    this.PropertyChanged?.Invoke(this, new global::System.ComponentModel.PropertyChangedEventArgs(propertyName));");
                sb.Append(memberIndent).AppendLine("}");
                sb.AppendLine();
                sb.Append(memberIndent).Append(helperAccess).AppendLine(" bool SetProperty<T>(ref T field, T value, [global::System.Runtime.CompilerServices.CallerMemberName] string? propertyName = null)");
                sb.Append(memberIndent).AppendLine("{");
                sb.Append(memberIndent).AppendLine("    if (global::System.Collections.Generic.EqualityComparer<T>.Default.Equals(field, value))");
                sb.Append(memberIndent).AppendLine("    {");
                sb.Append(memberIndent).AppendLine("        return false;");
                sb.Append(memberIndent).AppendLine("    }");
                sb.Append(memberIndent).AppendLine("    field = value;");
                sb.Append(memberIndent).AppendLine("    this.OnPropertyChanged(propertyName);");
                sb.Append(memberIndent).AppendLine("    return true;");
                sb.Append(memberIndent).AppendLine("}");
                sb.AppendLine();
            }

            foreach (ObservableFieldModel field in model.ObservableFields)
            {
                sb.Append(memberIndent).Append("public ").Append(field.FieldType).Append(' ').AppendLine(field.PropertyName);
                sb.Append(memberIndent).AppendLine("{");
                sb.Append(memberIndent).Append("    get => this.").Append(field.FieldName).AppendLine(";");
                sb.Append(memberIndent).AppendLine("    set");
                sb.Append(memberIndent).AppendLine("    {");
                sb.Append(memberIndent).Append("        if (!global::System.Collections.Generic.EqualityComparer<").Append(field.FieldType).Append(">.Default.Equals(this.").Append(field.FieldName).AppendLine(", value))");
                sb.Append(memberIndent).AppendLine("        {");
                sb.Append(memberIndent).Append("            this.On").Append(field.PropertyName).Append("Changing(this.").Append(field.FieldName).AppendLine(", value);");
                sb.Append(memberIndent).Append("            this.").Append(field.FieldName).AppendLine(" = value;");
                sb.Append(memberIndent).Append("            this.OnPropertyChanged(\"").Append(field.PropertyName).AppendLine("\");");
                foreach (string extraProp in field.AlsoNotifyProperties)
                {
                    sb.Append(memberIndent).Append("            this.OnPropertyChanged(\"").Append(extraProp).AppendLine("\");");
                }
                sb.Append(memberIndent).Append("            this.On").Append(field.PropertyName).AppendLine("Changed(value);");
                sb.Append(memberIndent).AppendLine("        }");
                sb.Append(memberIndent).AppendLine("    }");
                sb.Append(memberIndent).AppendLine("}");
                sb.AppendLine();
                sb.Append(memberIndent).Append("partial void On").Append(field.PropertyName).Append("Changing(").Append(field.FieldType).Append(" oldValue, ").Append(field.FieldType).AppendLine(" newValue);");
                sb.Append(memberIndent).Append("partial void On").Append(field.PropertyName).Append("Changed(").Append(field.FieldType).AppendLine(" value);");
                sb.AppendLine();
            }

            foreach (RelayCommandModel cmd in model.RelayCommands)
            {
                string cmdType = cmd.ParameterType is null
                    ? "global::System.Windows.Markup.RelayCommand"
                    : $"global::System.Windows.Markup.RelayCommand<{cmd.ParameterType}>";

                sb.Append(memberIndent).Append("private ").Append(cmdType).Append("? ").Append(cmd.CommandFieldName).AppendLine(";");

                string canExecArg = "null";
                if (!string.IsNullOrEmpty(cmd.CanExecuteMember))
                {
                    if (cmd.CanExecuteIsProperty)
                    {
                        canExecArg = cmd.ParameterType is null
                            ? $"() => this.{cmd.CanExecuteMember}"
                            : $"_ => this.{cmd.CanExecuteMember}";
                    }
                    else
                    {
                        canExecArg = $"this.{cmd.CanExecuteMember}";
                    }
                }

                sb.Append(memberIndent)
                  .Append("public ")
                  .Append(cmdType)
                  .Append(' ')
                  .Append(cmd.CommandPropertyName)
                  .Append(" => this.")
                  .Append(cmd.CommandFieldName)
                  .Append(" ??= new ")
                  .Append(cmdType)
                  .Append("(this.")
                  .Append(cmd.MethodName)
                  .Append(", ")
                  .Append(canExecArg)
                  .AppendLine(");");
                sb.AppendLine();
            }

            sb.Append(indent).AppendLine("}");
            if (hasNamespace)
            {
                sb.AppendLine("}");
            }

            return sb.ToString();
        }

        private static string? GenerateModuleInitializer(IEnumerable<TypeModel> models)
        {
            var validModels = models.Where(static m => !m.IsGeneric && !m.IsAbstract).ToList();
            if (validModels.Count == 0)
            {
                return null;
            }

            var sb = new StringBuilder();
            sb.AppendLine("// <auto-generated />");
            sb.AppendLine("#nullable enable");
            sb.AppendLine();
            sb.AppendLine("namespace CSUI.Generated");
            sb.AppendLine("{");
            sb.AppendLine("    internal static class CsuiGeneratedModuleInitializer");
            sb.AppendLine("    {");
            sb.AppendLine("        [global::System.Runtime.CompilerServices.ModuleInitializer]");
            sb.AppendLine("        internal static void Initialize()");
            sb.AppendLine("        {");

            foreach (TypeModel model in validModels)
            {
                if (model.IsObservableObject || model.IsBindable)
                {
                    foreach (BindablePropertyModel prop in model.PublicProperties)
                    {
                        if (!prop.CanRead)
                        {
                            continue;
                        }

                        string setterExpr = prop.CanWrite && !model.IsStruct
                            ? $"static (s, v) => s.{prop.PropertyName} = v"
                            : "null";

                        sb.Append("            global::System.Windows.Data.PropertyAccessorRegistry.Register<")
                          .Append(model.FullTypeName)
                          .Append(", ")
                          .Append(prop.PropertyType)
                          .Append(">(\"")
                          .Append(prop.PropertyName)
                          .Append("\", static s => s.")
                          .Append(prop.PropertyName)
                          .Append(", ")
                          .Append(setterExpr)
                          .AppendLine(");");
                    }

                    foreach (BindableIndexerModel idx in model.Indexers)
                    {
                        string setterExpr = idx.CanWrite && !model.IsStruct
                            ? "static (s, i, v) => s[i] = v"
                            : "null";

                        sb.Append("            global::System.Windows.Data.PropertyAccessorRegistry.RegisterIndexer<")
                          .Append(model.FullTypeName)
                          .Append(", ")
                          .Append(idx.IndexType)
                          .Append(", ")
                          .Append(idx.ValueType)
                          .AppendLine($">(static (s, i) => s[i], {setterExpr});");
                    }
                }

                if (model.IsXamlType && model.HasParameterlessCtor && !model.IsStruct)
                {
                    sb.Append("            global::CSUI.Markup.XamlMarkup.RegisterType<")
                      .Append(model.FullTypeName)
                      .AppendLine(">();");

                    foreach ((string shortName, string dpFieldName) in model.DeclaredXamlDependencyProperties)
                    {
                        sb.Append("            global::CSUI.Markup.XamlMarkup.RegisterProperty(typeof(")
                          .Append(model.FullTypeName)
                          .Append("), \"")
                          .Append(shortName)
                          .Append("\", ")
                          .Append(model.FullTypeName)
                          .Append('.')
                          .Append(dpFieldName)
                          .AppendLine(");");
                    }

                    foreach (BindablePropertyModel clrProp in model.DeclaredXamlClrProperties)
                    {
                        sb.Append("            global::CSUI.Markup.XamlMarkup.RegisterClrProperty<")
                          .Append(model.FullTypeName)
                          .Append(", ")
                          .Append(clrProp.PropertyType)
                          .Append(">(\"")
                          .Append(clrProp.PropertyName)
                          .Append("\", static (t, v) => t.")
                          .Append(clrProp.PropertyName)
                          .Append(" = v, static t => t.")
                          .Append(clrProp.PropertyName)
                          .AppendLine(");");
                    }
                }
            }

            sb.AppendLine("        }");
            sb.AppendLine("    }");
            sb.AppendLine("}");
            return sb.ToString();
        }

        private static string? TryGenerateXamlPartialClass(string filePath, string xamlContent)
        {
            XDocument doc;
            try
            {
                doc = XDocument.Parse(xamlContent);
            }
            catch
            {
                return null;
            }

            XElement? root = doc.Root;
            if (root is null)
            {
                return null;
            }

            const string XamlNs = "http://schemas.microsoft.com/winfx/2006/xaml";
            string? xClass = root.Attribute(XName.Get("Class", XamlNs))?.Value ?? root.Attribute("Class")?.Value;
            if (string.IsNullOrWhiteSpace(xClass))
            {
                return null;
            }

            int lastDot = xClass!.LastIndexOf('.');
            string ns = lastDot > 0 ? xClass.Substring(0, lastDot) : string.Empty;
            string className = lastDot > 0 ? xClass.Substring(lastDot + 1) : xClass;

            var namedElements = new List<(string Name, string TagName)>();
            CollectNamedElements(root, XamlNs, namedElements);

            var sb = new StringBuilder();
            sb.AppendLine("// <auto-generated />");
            sb.AppendLine("#nullable enable");
            sb.AppendLine("using System;");
            sb.AppendLine("using System.Windows;");
            sb.AppendLine("using System.Windows.Controls;");
            sb.AppendLine("using System.Windows.Markup;");
            sb.AppendLine();

            bool hasNs = !string.IsNullOrEmpty(ns);
            if (hasNs)
            {
                sb.Append("namespace ").Append(ns).AppendLine();
                sb.AppendLine("{");
            }

            string indent = hasNs ? "    " : "";
            string memberIndent = indent + "    ";

            sb.Append(indent).Append("public partial class ").AppendLine(className);
            sb.Append(indent).AppendLine("{");
            sb.Append(memberIndent).AppendLine("private bool _contentLoaded;");
            sb.AppendLine();

            foreach ((string elemName, string tagName) in namedElements)
            {
                sb.Append(memberIndent).Append("internal ").Append(tagName).Append(' ').Append(elemName).AppendLine(" = null!;");
            }

            if (namedElements.Count > 0)
            {
                sb.AppendLine();
            }

            string escapedXaml = xamlContent.Replace("\"", "\"\"");
            sb.Append(memberIndent).Append("private const string __GeneratedXamlSource = @\"").Append(escapedXaml).AppendLine("\";");
            sb.AppendLine();

            sb.Append(memberIndent).AppendLine("public void InitializeComponent()");
            sb.Append(memberIndent).AppendLine("{");
            sb.Append(memberIndent).AppendLine("    if (this._contentLoaded)");
            sb.Append(memberIndent).AppendLine("    {");
            sb.Append(memberIndent).AppendLine("        return;");
            sb.Append(memberIndent).AppendLine("    }");
            sb.Append(memberIndent).AppendLine("    this._contentLoaded = true;");
            sb.Append(memberIndent).AppendLine("    global::CSUI.Markup.XamlMarkup.LoadInto(this, __GeneratedXamlSource, out global::CSUI.Markup.XamlLoadContext __ctx);");

            foreach ((string elemName, string tagName) in namedElements)
            {
                sb.Append(memberIndent)
                  .Append("    this.")
                  .Append(elemName)
                  .Append(" = __ctx.FindName<")
                  .Append(tagName)
                  .Append(">(\"")
                  .Append(elemName)
                  .AppendLine("\")!;");
            }

            sb.Append(memberIndent).AppendLine("    this.OnComponentInitialized();");
            sb.Append(memberIndent).AppendLine("}");
            sb.AppendLine();
            sb.Append(memberIndent).AppendLine("partial void OnComponentInitialized();");
            sb.Append(indent).AppendLine("}");

            if (hasNs)
            {
                sb.AppendLine("}");
            }

            return sb.ToString();
        }

        private static void CollectNamedElements(XElement element, string xamlNs, List<(string Name, string TagName)> results)
        {
            foreach (XElement child in element.Elements())
            {
                string localName = child.Name.LocalName;
                if (localName.Contains('.'))
                {
                    // Property element syntax (e.g., <MarkupCanvas.Resources>) - skip Resources/Style/Template inner names
                    if (localName.EndsWith(".Resources", StringComparison.Ordinal) ||
                        localName.EndsWith(".Style", StringComparison.Ordinal) ||
                        localName.EndsWith(".Template", StringComparison.Ordinal))
                    {
                        continue;
                    }
                    CollectNamedElements(child, xamlNs, results);
                    continue;
                }

                if (localName == "Style" || localName == "ControlTemplate" || localName == "ResourceDictionary")
                {
                    continue;
                }

                string? xName = child.Attribute(XName.Get("Name", xamlNs))?.Value ?? child.Attribute("Name")?.Value;
                if (!string.IsNullOrWhiteSpace(xName))
                {
                    results.Add((xName!, localName));
                }

                CollectNamedElements(child, xamlNs, results);
            }
        }

        private sealed record TypeModel(
            string Namespace,
            string TypeName,
            string FullTypeName,
            string AccessibilityKeyword,
            bool IsStruct,
            bool IsSealed,
            bool IsPartial,
            bool IsAbstract,
            bool IsGeneric,
            bool HasParameterlessCtor,
            bool IsObservableObject,
            bool IsBindable,
            bool IsXamlType,
            bool AlreadyImplementsInpc,
            List<ObservableFieldModel> ObservableFields,
            List<RelayCommandModel> RelayCommands,
            List<BindablePropertyModel> PublicProperties,
            List<BindableIndexerModel> Indexers,
            List<BindablePropertyModel> DeclaredXamlClrProperties,
            List<(string ShortName, string FieldName)> DeclaredXamlDependencyProperties);

        private sealed record ObservableFieldModel(
            string FieldName,
            string PropertyName,
            string FieldType,
            List<string> AlsoNotifyProperties);

        private sealed record RelayCommandModel(
            string MethodName,
            string CommandPropertyName,
            string CommandFieldName,
            string? ParameterType,
            string? CanExecuteMember,
            bool CanExecuteIsProperty);

        private sealed record BindablePropertyModel(
            string PropertyName,
            string PropertyType,
            bool CanRead,
            bool CanWrite);

        private sealed record BindableIndexerModel(
            string IndexType,
            string ValueType,
            bool CanWrite);
    }
}
