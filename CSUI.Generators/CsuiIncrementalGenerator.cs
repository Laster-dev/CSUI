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

            IncrementalValueProvider<Compilation> compilationProvider = context.CompilationProvider;

            context.RegisterSourceOutput(xamlFiles.Combine(compilationProvider), static (spc, pair) =>
            {
                AdditionalText xamlFile = pair.Left;
                Compilation compilation = pair.Right;

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

                string? generated = TryGenerateXamlPartialClass(xamlFile.Path, rawXaml, compilation);
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

        private static string? TryGenerateXamlPartialClass(string filePath, string xamlContent, Compilation compilation)
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

            var namedElements = new List<(string Name, string TagName, string FullTypeName)>();
            CollectNamedElements(root, XamlNs, ns, compilation, namedElements);

            // Try compiling XAML directly into pure strongly-typed C# (zero reflection, zero XamlMarkup root)
            string? compiledBody = TryCompileXamlInitializeBody(root, xClass, ns, XamlNs, compilation);

            var sb = new StringBuilder();
            sb.AppendLine("// <auto-generated />");
            sb.AppendLine(compiledBody is not null
                ? "// Compiled XAML -> direct C# (zero runtime XML / zero XamlMarkup root)"
                : "// Fallback runtime XAML loader");
            sb.AppendLine("#nullable enable");
            sb.AppendLine("using System;");
            sb.AppendLine("using System.Windows;");
            sb.AppendLine("using System.Windows.Controls;");
            sb.AppendLine("using System.Windows.Controls.Primitives;");
            sb.AppendLine("using System.Windows.Media;");
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

            foreach ((string elemName, string tagName, string fullTypeName) in namedElements)
            {
                string fieldType = compiledBody is not null && !string.IsNullOrEmpty(fullTypeName) ? fullTypeName : tagName;
                sb.Append(memberIndent).Append("internal ").Append(fieldType).Append(' ').Append(elemName).AppendLine(" = null!;");
            }

            if (namedElements.Count > 0)
            {
                sb.AppendLine();
            }

            if (compiledBody is null)
            {
                string escapedXaml = xamlContent.Replace("\"", "\"\"");
                sb.Append(memberIndent).Append("private const string __GeneratedXamlSource = @\"").Append(escapedXaml).AppendLine("\";");
                sb.AppendLine();
            }

            sb.Append(memberIndent).AppendLine("public void InitializeComponent()");
            sb.Append(memberIndent).AppendLine("{");
            sb.Append(memberIndent).AppendLine("    if (this._contentLoaded)");
            sb.Append(memberIndent).AppendLine("    {");
            sb.Append(memberIndent).AppendLine("        return;");
            sb.Append(memberIndent).AppendLine("    }");
            sb.Append(memberIndent).AppendLine("    this._contentLoaded = true;");

            if (compiledBody is not null)
            {
                using var reader = new StringReader(compiledBody);
                string? line;
                while ((line = reader.ReadLine()) is not null)
                {
                    if (line.Length == 0)
                    {
                        sb.AppendLine();
                    }
                    else
                    {
                        sb.Append(memberIndent).Append("    ").AppendLine(line);
                    }
                }
            }
            else
            {
                sb.Append(memberIndent).AppendLine("    global::CSUI.Markup.XamlMarkup.LoadInto(this, __GeneratedXamlSource, out global::CSUI.Markup.XamlLoadContext __ctx);");

                foreach ((string elemName, string tagName, _) in namedElements)
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

        private static readonly string[] s_defaultWpfNamespaces =
        {
            "System.Windows",
            "System.Windows.Controls",
            "System.Windows.Controls.Primitives",
            "System.Windows.Shapes",
            "System.Windows.Media",
            "System.Windows.Media.Effects",
            "System.Windows.Media.Animation",
            "System.Windows.Media.Imaging",
            "System.Windows.Media.Media3D",
            "System.Windows.Documents",
            "System.Windows.Input",
            "System.Windows.Data",
            "System.Windows.Shell",
            "CSUI.Core",
        };

        private static string? TryCompileXamlInitializeBody(
            XElement root,
            string xClass,
            string classNamespace,
            string xamlNs,
            Compilation compilation)
        {
            INamedTypeSymbol? rootType = compilation.GetTypeByMetadataName(xClass);
            if (rootType is null)
            {
                return null;
            }

            var body = new StringBuilder();
            var bindingSb = new StringBuilder();
            int varCounter = 0;
            if (!TryEmitElementPopulation(body, bindingSb, root, "this", rootType, isRoot: true, classNamespace, xamlNs, compilation, ref varCounter))
            {
                return null;
            }

            if (bindingSb.Length > 0)
            {
                body.Append(bindingSb);
            }

            return body.ToString();
        }

        private static bool TryEmitElementPopulation(
            StringBuilder sb,
            StringBuilder bindingSb,
            XElement el,
            string targetExpr,
            INamedTypeSymbol targetType,
            bool isRoot,
            string classNamespace,
            string xamlNs,
            Compilation compilation,
            ref int varCounter)
        {
            // 1. Emit attributes on element
            foreach (XAttribute attr in el.Attributes())
            {
                if (attr.IsNamespaceDeclaration)
                {
                    continue;
                }

                string attrNs = attr.Name.NamespaceName;
                string attrLocal = attr.Name.LocalName;

                if (attrNs == xamlNs)
                {
                    if (attrLocal == "Class" || attrLocal == "ClassModifier" || attrLocal == "FieldModifier")
                    {
                        continue;
                    }
                    if (attrLocal == "Name")
                    {
                        if (!isRoot)
                        {
                            sb.Append("this.").Append(attr.Value).Append(" = ").Append(targetExpr).AppendLine(";");
                        }
                        if (FindProperty(targetType, "Name") is IPropertySymbol nameProp && nameProp.SetMethod is not null)
                        {
                            sb.Append(targetExpr).Append(".Name = ").Append(ToCSharpStringLiteral(attr.Value)).AppendLine(";");
                        }
                        continue;
                    }
                    return false;
                }

                if (attrNs == "http://schemas.openxmlformats.org/markup-compatibility/2006" && attrLocal == "Ignorable")
                {
                    continue;
                }

                if (!string.IsNullOrEmpty(attrNs))
                {
                    return false;
                }

                if (attrLocal == "Name")
                {
                    if (!isRoot)
                    {
                        sb.Append("this.").Append(attr.Value).Append(" = ").Append(targetExpr).AppendLine(";");
                    }
                }

                // Attached property syntax: Owner.Prop="..."
                int dotIdx = attrLocal.IndexOf('.');
                if (dotIdx > 0)
                {
                    string ownerName = attrLocal.Substring(0, dotIdx);
                    string propName = attrLocal.Substring(dotIdx + 1);
                    INamedTypeSymbol? ownerType = ResolveElementType(ownerName, el.Name.NamespaceName, classNamespace, compilation);
                    if (ownerType is null)
                    {
                        return false;
                    }

                    if (!TryEmitAttachedPropertyAssignment(sb, targetExpr, ownerType, propName, attr.Value, classNamespace, compilation))
                    {
                        return false;
                    }
                    continue;
                }

                // Instance property or event
                if (FindProperty(targetType, attrLocal) is IPropertySymbol prop && prop.SetMethod is not null && prop.SetMethod.DeclaredAccessibility == Accessibility.Public)
                {
                    string trimmed = attr.Value.Trim();
                    if (trimmed.StartsWith("{", StringComparison.Ordinal) && trimmed.EndsWith("}", StringComparison.Ordinal))
                    {
                        StringBuilder targetOut = trimmed.StartsWith("{Binding", StringComparison.Ordinal) ? bindingSb : sb;
                        if (!TryEmitMarkupExtensionAssignment(targetOut, targetExpr, targetType, prop, trimmed, el.Name.NamespaceName, classNamespace, compilation))
                        {
                            return false;
                        }
                    }
                    else
                    {
                        if (!TryConvertLiteralToCSharp(attr.Value, prop.Type, out string valueExpr))
                        {
                            return false;
                        }
                        sb.Append(targetExpr).Append('.').Append(prop.Name).Append(" = ").Append(valueExpr).AppendLine(";");
                    }
                    continue;
                }

                if (FindEvent(targetType, attrLocal) is IEventSymbol evt)
                {
                    sb.Append(targetExpr).Append('.').Append(evt.Name).Append(" += this.").Append(attr.Value.Trim()).AppendLine(";");
                    continue;
                }

                return false;
            }

            // 2. Emit child elements / text content
            bool hasElementChildren = false;
            foreach (XNode node in el.Nodes())
            {
                if (node is XElement childEl)
                {
                    hasElementChildren = true;
                    string childLocal = childEl.Name.LocalName;
                    int dotIdx = childLocal.IndexOf('.');
                    if (dotIdx > 0)
                    {
                        // Property element syntax: <Owner.Property>
                        string ownerName = childLocal.Substring(0, dotIdx);
                        string propName = childLocal.Substring(dotIdx + 1);
                        if (propName == "Resources" || propName == "Style" || propName == "Template")
                        {
                            return false;
                        }

                        IPropertySymbol? prop = FindProperty(targetType, propName);
                        if (prop is null)
                        {
                            return false;
                        }

                        bool isCollection = IsCollectionLike(prop.Type);
                        foreach (XElement itemEl in childEl.Elements())
                        {
                            if (!TryCreateAndEmitChildElement(sb, bindingSb, itemEl, classNamespace, xamlNs, compilation, ref varCounter, out string childVar, out _))
                            {
                                return false;
                            }
                            if (isCollection)
                            {
                                sb.Append(targetExpr).Append('.').Append(prop.Name).Append(".Add(").Append(childVar).AppendLine(");");
                            }
                            else
                            {
                                if (prop.SetMethod is null || prop.SetMethod.DeclaredAccessibility != Accessibility.Public)
                                {
                                    return false;
                                }
                                sb.Append(targetExpr).Append('.').Append(prop.Name).Append(" = ").Append(childVar).AppendLine(";");
                            }
                        }
                        continue;
                    }

                    if (childLocal == "Style" || childLocal == "ControlTemplate" || childLocal == "ResourceDictionary" || childLocal == "DataTemplate")
                    {
                        return false;
                    }

                    if (!TryCreateAndEmitChildElement(sb, bindingSb, childEl, classNamespace, xamlNs, compilation, ref varCounter, out string childVarName, out _))
                    {
                        return false;
                    }

                    if (!TryAddContentChild(sb, targetExpr, targetType, childVarName, compilation))
                    {
                        return false;
                    }
                }
            }

            if (!hasElementChildren)
            {
                string innerText = string.Concat(el.Nodes().OfType<XText>().Select(static t => t.Value));
                if (!string.IsNullOrWhiteSpace(innerText))
                {
                    if (!TryAssignContentText(sb, targetExpr, targetType, innerText.Trim()))
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        private static bool TryCreateAndEmitChildElement(
            StringBuilder sb,
            StringBuilder bindingSb,
            XElement childEl,
            string classNamespace,
            string xamlNs,
            Compilation compilation,
            ref int varCounter,
            out string varName,
            out INamedTypeSymbol childType)
        {
            varName = string.Empty;
            INamedTypeSymbol? resolved = ResolveElementType(childEl.Name.LocalName, childEl.Name.NamespaceName, classNamespace, compilation);
            if (resolved is null || resolved.IsAbstract || !HasPublicParameterlessCtor(resolved))
            {
                childType = null!;
                return false;
            }

            childType = resolved;
            varCounter++;
            varName = "__csui" + varCounter.ToString(System.Globalization.CultureInfo.InvariantCulture);
            string fullType = resolved.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
            sb.Append("var ").Append(varName).Append(" = new ").Append(fullType).AppendLine("();");

            return TryEmitElementPopulation(sb, bindingSb, childEl, varName, resolved, isRoot: false, classNamespace, xamlNs, compilation, ref varCounter);
        }

        private static bool TryAddContentChild(
            StringBuilder sb,
            string targetExpr,
            INamedTypeSymbol targetType,
            string childExpr,
            Compilation compilation)
        {
            string? contentPropName = FindContentPropertyName(targetType);
            if (!string.IsNullOrEmpty(contentPropName))
            {
                IPropertySymbol? prop = FindProperty(targetType, contentPropName!);
                if (prop is not null)
                {
                    if (IsCollectionLike(prop.Type))
                    {
                        sb.Append(targetExpr).Append('.').Append(prop.Name).Append(".Add(").Append(childExpr).AppendLine(");");
                        return true;
                    }
                    if (prop.SetMethod is not null && prop.SetMethod.DeclaredAccessibility == Accessibility.Public)
                    {
                        sb.Append(targetExpr).Append('.').Append(prop.Name).Append(" = ").Append(childExpr).AppendLine(";");
                        return true;
                    }
                }
            }

            if (InheritsFrom(targetType, "System.Windows.Controls.Panel"))
            {
                sb.Append(targetExpr).Append(".Children.Add(").Append(childExpr).AppendLine(");");
                return true;
            }
            if (InheritsFrom(targetType, "System.Windows.Controls.Decorator"))
            {
                sb.Append(targetExpr).Append(".Child = ").Append(childExpr).AppendLine(";");
                return true;
            }
            if (InheritsFrom(targetType, "System.Windows.Controls.ContentControl"))
            {
                sb.Append(targetExpr).Append(".Content = ").Append(childExpr).AppendLine(";");
                return true;
            }
            if (InheritsFrom(targetType, "System.Windows.Controls.ItemsControl"))
            {
                sb.Append(targetExpr).Append(".Items.Add(").Append(childExpr).AppendLine(");");
                return true;
            }
            if (IsCollectionLike(targetType))
            {
                sb.Append(targetExpr).Append(".Add(").Append(childExpr).AppendLine(");");
                return true;
            }

            return false;
        }

        private static bool TryAssignContentText(
            StringBuilder sb,
            string targetExpr,
            INamedTypeSymbol targetType,
            string text)
        {
            string? contentPropName = FindContentPropertyName(targetType);
            if (!string.IsNullOrEmpty(contentPropName))
            {
                IPropertySymbol? prop = FindProperty(targetType, contentPropName!);
                if (prop is not null && prop.SetMethod is not null && prop.SetMethod.DeclaredAccessibility == Accessibility.Public &&
                    TryConvertLiteralToCSharp(text, prop.Type, out string expr))
                {
                    sb.Append(targetExpr).Append('.').Append(prop.Name).Append(" = ").Append(expr).AppendLine(";");
                    return true;
                }
            }

            if (FindProperty(targetType, "Text") is IPropertySymbol textProp && textProp.SetMethod is not null)
            {
                sb.Append(targetExpr).Append(".Text = ").Append(ToCSharpStringLiteral(text)).AppendLine(";");
                return true;
            }
            if (FindProperty(targetType, "Content") is IPropertySymbol contentProp && contentProp.SetMethod is not null)
            {
                sb.Append(targetExpr).Append(".Content = ").Append(ToCSharpStringLiteral(text)).AppendLine(";");
                return true;
            }

            return false;
        }

        private static bool TryEmitAttachedPropertyAssignment(
            StringBuilder sb,
            string targetExpr,
            INamedTypeSymbol ownerType,
            string propName,
            string rawValue,
            string classNamespace,
            Compilation compilation)
        {
            string setterName = "Set" + propName;
            IMethodSymbol? setMethod = null;
            for (INamedTypeSymbol? cur = ownerType; cur is not null; cur = cur.BaseType)
            {
                foreach (ISymbol member in cur.GetMembers(setterName))
                {
                    if (member is IMethodSymbol m && m.IsStatic && m.DeclaredAccessibility == Accessibility.Public && m.Parameters.Length == 2)
                    {
                        setMethod = m;
                        break;
                    }
                }
                if (setMethod is not null) break;
            }

            if (setMethod is null)
            {
                return false;
            }

            string trimmed = rawValue.Trim();
            if (trimmed == "{x:Null}")
            {
                if (setMethod.Parameters[1].Type.IsValueType && setMethod.Parameters[1].Type.OriginalDefinition.SpecialType != SpecialType.System_Nullable_T)
                {
                    return false;
                }
                sb.Append(ownerType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat))
                  .Append('.').Append(setterName).Append('(').Append(targetExpr).AppendLine(", null);");
                return true;
            }

            if (trimmed.StartsWith("{", StringComparison.Ordinal))
            {
                return false;
            }

            if (!TryConvertLiteralToCSharp(rawValue, setMethod.Parameters[1].Type, out string valExpr))
            {
                return false;
            }

            sb.Append(ownerType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat))
              .Append('.').Append(setterName).Append('(').Append(targetExpr).Append(", ").Append(valExpr).AppendLine(");");
            return true;
        }

        private static bool TryEmitMarkupExtensionAssignment(
            StringBuilder sb,
            string targetExpr,
            INamedTypeSymbol targetType,
            IPropertySymbol prop,
            string expr,
            string defaultXmlNs,
            string classNamespace,
            Compilation compilation)
        {
            if (string.Equals(expr, "{x:Null}", StringComparison.Ordinal))
            {
                if (prop.Type.IsValueType && prop.Type.OriginalDefinition.SpecialType != SpecialType.System_Nullable_T)
                {
                    return false;
                }
                sb.Append(targetExpr).Append('.').Append(prop.Name).AppendLine(" = null;");
                return true;
            }

            string inner = expr.Substring(1, expr.Length - 2).Trim();
            if (inner.StartsWith("x:Static ", StringComparison.Ordinal))
            {
                string memberRef = inner.Substring("x:Static ".Length).Trim();
                int dot = memberRef.LastIndexOf('.');
                if (dot <= 0) return false;
                string typePart = memberRef.Substring(0, dot);
                int colon = typePart.IndexOf(':');
                if (colon >= 0) typePart = typePart.Substring(colon + 1);
                string memberName = memberRef.Substring(dot + 1);
                INamedTypeSymbol? staticType = ResolveElementType(typePart, defaultXmlNs, classNamespace, compilation);
                if (staticType is null) return false;
                sb.Append(targetExpr).Append('.').Append(prop.Name).Append(" = ")
                  .Append(staticType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat))
                  .Append('.').Append(memberName).AppendLine(";");
                return true;
            }

            if (inner.Equals("Binding", StringComparison.Ordinal) || inner.StartsWith("Binding ", StringComparison.Ordinal))
            {
                var dpField = FindDependencyPropertyField(targetType, prop.Name + "Property");
                if (dpField is null)
                {
                    return false;
                }

                string args = inner.Length > 7 ? inner.Substring(7).Trim() : string.Empty;
                string? path = null;
                string? mode = null;
                string? updateSourceTrigger = null;
                string? elementName = null;
                string? stringFormat = null;
                string? fallbackValue = null;
                string? targetNullValue = null;

                if (!string.IsNullOrEmpty(args))
                {
                    foreach (string rawPart in SplitMarkupArgs(args))
                    {
                        string part = rawPart.Trim();
                        if (string.IsNullOrEmpty(part)) continue;
                        int eq = part.IndexOf('=');
                        if (eq < 0)
                        {
                            path ??= part;
                            continue;
                        }
                        string k = part.Substring(0, eq).Trim();
                        string v = part.Substring(eq + 1).Trim();
                        if (v.Length >= 2 && ((v[0] == '\'' && v[v.Length - 1] == '\'') || (v[0] == '"' && v[v.Length - 1] == '"')))
                        {
                            v = v.Substring(1, v.Length - 2);
                        }
                        switch (k)
                        {
                            case "Path":
                                path = v;
                                break;
                            case "Mode":
                                mode = v;
                                break;
                            case "UpdateSourceTrigger":
                                updateSourceTrigger = v;
                                break;
                            case "ElementName":
                                elementName = v;
                                break;
                            case "StringFormat":
                                stringFormat = v;
                                break;
                            case "FallbackValue":
                                fallbackValue = v;
                                break;
                            case "TargetNullValue":
                                targetNullValue = v;
                                break;
                            default:
                                // RelativeSource, Converter, Source, etc. -> fallback to runtime parser
                                return false;
                        }
                    }
                }

                string dpOwner = dpField.ContainingType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
                sb.Append("global::System.Windows.Data.BindingOperations.SetBinding(")
                  .Append(targetExpr).Append(", ")
                  .Append(dpOwner).Append('.').Append(dpField.Name)
                  .Append(", new global::System.Windows.Data.Binding(");
                if (path is not null)
                {
                    sb.Append(ToCSharpStringLiteral(path));
                }
                sb.Append(')');

                var inits = new List<string>();
                if (mode is not null)
                {
                    inits.Add("Mode = global::System.Windows.Data.BindingMode." + mode);
                }
                if (updateSourceTrigger is not null)
                {
                    inits.Add("UpdateSourceTrigger = global::System.Windows.Data.UpdateSourceTrigger." + updateSourceTrigger);
                }
                if (elementName is not null)
                {
                    inits.Add("ElementName = " + ToCSharpStringLiteral(elementName));
                }
                if (stringFormat is not null)
                {
                    inits.Add("StringFormat = " + ToCSharpStringLiteral(stringFormat));
                }
                if (fallbackValue is not null)
                {
                    inits.Add("FallbackValue = " + ToCSharpStringLiteral(fallbackValue));
                }
                if (targetNullValue is not null)
                {
                    inits.Add("TargetNullValue = " + ToCSharpStringLiteral(targetNullValue));
                }

                if (inits.Count > 0)
                {
                    sb.Append(" { ").Append(string.Join(", ", inits)).Append(" }");
                }
                sb.AppendLine(");");
                return true;
            }

            return false;
        }

        private static List<string> SplitMarkupArgs(string args)
        {
            var result = new List<string>();
            int start = 0;
            bool inSingle = false;
            bool inDouble = false;
            for (int i = 0; i < args.Length; i++)
            {
                char c = args[i];
                if (c == '\'' && !inDouble) inSingle = !inSingle;
                else if (c == '"' && !inSingle) inDouble = !inDouble;
                else if (c == ',' && !inSingle && !inDouble)
                {
                    result.Add(args.Substring(start, i - start));
                    start = i + 1;
                }
            }
            if (start <= args.Length)
            {
                result.Add(args.Substring(start));
            }
            return result;
        }

        private static INamedTypeSymbol? ResolveElementType(
            string localName,
            string xmlNs,
            string classNamespace,
            Compilation compilation)
        {
            if (xmlNs.StartsWith("clr-namespace:", StringComparison.Ordinal))
            {
                string rest = xmlNs.Substring("clr-namespace:".Length);
                int semi = rest.IndexOf(';');
                string clrNs = semi >= 0 ? rest.Substring(0, semi) : rest;
                return compilation.GetTypeByMetadataName(clrNs + "." + localName);
            }

            if (!string.IsNullOrEmpty(classNamespace))
            {
                INamedTypeSymbol? inSameNs = compilation.GetTypeByMetadataName(classNamespace + "." + localName);
                if (inSameNs is not null) return inSameNs;
            }

            foreach (string ns in s_defaultWpfNamespaces)
            {
                INamedTypeSymbol? found = compilation.GetTypeByMetadataName(ns + "." + localName);
                if (found is not null) return found;
            }

            return compilation.GetTypeByMetadataName(localName);
        }

        private static IPropertySymbol? FindProperty(INamedTypeSymbol type, string name)
        {
            for (INamedTypeSymbol? cur = type; cur is not null; cur = cur.BaseType)
            {
                foreach (ISymbol member in cur.GetMembers(name))
                {
                    if (member is IPropertySymbol prop && !prop.IsStatic)
                    {
                        return prop;
                    }
                }
            }
            return null;
        }

        private static IEventSymbol? FindEvent(INamedTypeSymbol type, string name)
        {
            for (INamedTypeSymbol? cur = type; cur is not null; cur = cur.BaseType)
            {
                foreach (ISymbol member in cur.GetMembers(name))
                {
                    if (member is IEventSymbol evt && !evt.IsStatic && evt.DeclaredAccessibility == Accessibility.Public)
                    {
                        return evt;
                    }
                }
            }
            return null;
        }

        private static IFieldSymbol? FindDependencyPropertyField(INamedTypeSymbol type, string fieldName)
        {
            for (INamedTypeSymbol? cur = type; cur is not null; cur = cur.BaseType)
            {
                foreach (ISymbol member in cur.GetMembers(fieldName))
                {
                    if (member is IFieldSymbol fld && fld.IsStatic && fld.DeclaredAccessibility == Accessibility.Public)
                    {
                        return fld;
                    }
                }
            }
            return null;
        }

        private static string? FindContentPropertyName(INamedTypeSymbol type)
        {
            for (INamedTypeSymbol? cur = type; cur is not null; cur = cur.BaseType)
            {
                foreach (AttributeData attr in cur.GetAttributes())
                {
                    if (attr.AttributeClass?.ToDisplayString() == "System.Windows.Markup.ContentPropertyAttribute" &&
                        attr.ConstructorArguments.Length > 0 &&
                        attr.ConstructorArguments[0].Value is string name &&
                        !string.IsNullOrEmpty(name))
                    {
                        return name;
                    }
                }
            }
            return null;
        }

        private static bool HasPublicParameterlessCtor(INamedTypeSymbol type)
        {
            foreach (IMethodSymbol ctor in type.InstanceConstructors)
            {
                if (ctor.Parameters.Length == 0 && ctor.DeclaredAccessibility == Accessibility.Public)
                {
                    return true;
                }
            }
            return false;
        }

        private static bool InheritsFrom(INamedTypeSymbol type, string fullMetadataName)
        {
            for (INamedTypeSymbol? cur = type; cur is not null; cur = cur.BaseType)
            {
                if (cur.ToDisplayString() == fullMetadataName)
                {
                    return true;
                }
            }
            return false;
        }

        private static bool IsCollectionLike(ITypeSymbol type)
        {
            if (type.SpecialType == SpecialType.System_String) return false;
            foreach (INamedTypeSymbol iface in type.AllInterfaces)
            {
                string name = iface.ToDisplayString();
                if (name == "System.Collections.IList" || name == "System.Collections.Generic.ICollection<T>" || iface.OriginalDefinition.ToDisplayString() == "System.Collections.Generic.ICollection<T>")
                {
                    return true;
                }
            }
            return false;
        }

        private static bool TryConvertLiteralToCSharp(string raw, ITypeSymbol targetType, out string expr)
        {
            expr = string.Empty;
            if (targetType is INamedTypeSymbol nullableNamed &&
                nullableNamed.OriginalDefinition.SpecialType == SpecialType.System_Nullable_T)
            {
                if (string.Equals(raw.Trim(), "{x:Null}", StringComparison.Ordinal))
                {
                    expr = "null";
                    return true;
                }
                targetType = nullableNamed.TypeArguments[0];
            }

            string trimmed = raw.Trim();
            if (targetType.SpecialType == SpecialType.System_Object)
            {
                expr = ToCSharpStringLiteral(raw);
                return true;
            }
            if (targetType.SpecialType == SpecialType.System_String)
            {
                expr = ToCSharpStringLiteral(raw);
                return true;
            }
            if (targetType.SpecialType == SpecialType.System_Char)
            {
                if (raw.Length != 1) return false;
                expr = ToCSharpCharLiteral(raw[0]);
                return true;
            }
            if (targetType.SpecialType == SpecialType.System_Boolean)
            {
                if (string.Equals(trimmed, "true", StringComparison.OrdinalIgnoreCase)) { expr = "true"; return true; }
                if (string.Equals(trimmed, "false", StringComparison.OrdinalIgnoreCase)) { expr = "false"; return true; }
                return false;
            }
            if (targetType.SpecialType == SpecialType.System_Int32)
            {
                if (int.TryParse(trimmed, System.Globalization.NumberStyles.Integer, System.Globalization.CultureInfo.InvariantCulture, out int v))
                {
                    expr = v.ToString(System.Globalization.CultureInfo.InvariantCulture);
                    return true;
                }
                return false;
            }
            if (targetType.SpecialType == SpecialType.System_Int64)
            {
                if (long.TryParse(trimmed, System.Globalization.NumberStyles.Integer, System.Globalization.CultureInfo.InvariantCulture, out long v))
                {
                    expr = v.ToString(System.Globalization.CultureInfo.InvariantCulture) + "L";
                    return true;
                }
                return false;
            }
            if (targetType.SpecialType == SpecialType.System_Byte)
            {
                if (byte.TryParse(trimmed, System.Globalization.NumberStyles.Integer, System.Globalization.CultureInfo.InvariantCulture, out byte v))
                {
                    expr = "(byte)" + v.ToString(System.Globalization.CultureInfo.InvariantCulture);
                    return true;
                }
                return false;
            }
            if (targetType.SpecialType == SpecialType.System_Double)
            {
                if (string.Equals(trimmed, "Auto", StringComparison.OrdinalIgnoreCase) || string.Equals(trimmed, "NaN", StringComparison.OrdinalIgnoreCase))
                {
                    expr = "double.NaN";
                    return true;
                }
                if (double.TryParse(trimmed, System.Globalization.NumberStyles.Float | System.Globalization.NumberStyles.AllowThousands, System.Globalization.CultureInfo.InvariantCulture, out double v))
                {
                    expr = v.ToString("R", System.Globalization.CultureInfo.InvariantCulture) + "d";
                    return true;
                }
                return false;
            }
            if (targetType.SpecialType == SpecialType.System_Single)
            {
                if (string.Equals(trimmed, "Auto", StringComparison.OrdinalIgnoreCase) || string.Equals(trimmed, "NaN", StringComparison.OrdinalIgnoreCase))
                {
                    expr = "float.NaN";
                    return true;
                }
                if (float.TryParse(trimmed, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out float v))
                {
                    expr = v.ToString("R", System.Globalization.CultureInfo.InvariantCulture) + "f";
                    return true;
                }
                return false;
            }

            if (targetType.TypeKind == TypeKind.Enum)
            {
                string enumFull = targetType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
                string[] parts = trimmed.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length == 0) return false;
                var resolvedMembers = new List<string>(parts.Length);
                foreach (string part in parts)
                {
                    string p = part.Trim();
                    IFieldSymbol? match = null;
                    foreach (ISymbol m in targetType.GetMembers())
                    {
                        if (m is IFieldSymbol f && f.HasConstantValue && string.Equals(f.Name, p, StringComparison.OrdinalIgnoreCase))
                        {
                            match = f;
                            break;
                        }
                    }
                    if (match is null) return false;
                    resolvedMembers.Add(enumFull + "." + match.Name);
                }
                expr = string.Join(" | ", resolvedMembers);
                return true;
            }

            string fullName = targetType.ToDisplayString();
            switch (fullName)
            {
                case "System.Windows.Media.Color":
                    return TryParseColorExpr(trimmed, out expr);
                case "System.Windows.Media.Brush":
                    if (TryParseColorExpr(trimmed, out string colorExpr))
                    {
                        expr = "new global::System.Windows.Media.SolidColorBrush(" + colorExpr + ")";
                        return true;
                    }
                    return false;
                case "System.Windows.Thickness":
                    return TryParseDoublesExpr(trimmed, static nums =>
                    {
                        if (nums.Length == 1) return $"new global::System.Windows.Thickness({nums[0]}d)";
                        if (nums.Length == 2) return $"new global::System.Windows.Thickness({nums[0]}d, {nums[1]}d, {nums[0]}d, {nums[1]}d)";
                        if (nums.Length == 4) return $"new global::System.Windows.Thickness({nums[0]}d, {nums[1]}d, {nums[2]}d, {nums[3]}d)";
                        return null;
                    }, out expr);
                case "System.Windows.CornerRadius":
                    return TryParseDoublesExpr(trimmed, static nums =>
                    {
                        if (nums.Length == 1) return $"new global::System.Windows.CornerRadius({nums[0]}d)";
                        if (nums.Length == 4) return $"new global::System.Windows.CornerRadius({nums[0]}d, {nums[1]}d, {nums[2]}d, {nums[3]}d)";
                        return null;
                    }, out expr);
                case "System.Windows.FontWeight":
                    if (int.TryParse(trimmed, System.Globalization.NumberStyles.Integer, System.Globalization.CultureInfo.InvariantCulture, out int fwNum))
                    {
                        expr = $"global::System.Windows.FontWeight.FromOpenTypeWeight({fwNum})";
                        return true;
                    }
                    string? weightProp = MatchStandardFontWeight(trimmed);
                    if (weightProp is not null)
                    {
                        expr = "global::System.Windows.FontWeights." + weightProp;
                        return true;
                    }
                    return false;
                case "System.Windows.Media.FontFamily":
                    expr = $"new global::System.Windows.Media.FontFamily({ToCSharpStringLiteral(trimmed)})";
                    return true;
                case "System.Windows.GridLength":
                    if (string.Equals(trimmed, "Auto", StringComparison.OrdinalIgnoreCase))
                    {
                        expr = "global::System.Windows.GridLength.Auto";
                        return true;
                    }
                    if (trimmed.EndsWith("*", StringComparison.Ordinal))
                    {
                        string numStr = trimmed.Substring(0, trimmed.Length - 1).Trim();
                        double stars = string.IsNullOrEmpty(numStr) ? 1.0 :
                            double.TryParse(numStr, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out double s) ? s : double.NaN;
                        if (double.IsNaN(stars)) return false;
                        expr = $"new global::System.Windows.GridLength({stars.ToString("R", System.Globalization.CultureInfo.InvariantCulture)}d, global::System.Windows.GridUnitType.Star)";
                        return true;
                    }
                    if (double.TryParse(trimmed, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out double px))
                    {
                        expr = $"new global::System.Windows.GridLength({px.ToString("R", System.Globalization.CultureInfo.InvariantCulture)}d, global::System.Windows.GridUnitType.Pixel)";
                        return true;
                    }
                    return false;
                case "System.Windows.Point":
                    return TryParseDoublesExpr(trimmed, static nums => nums.Length == 2 ? $"new global::System.Windows.Point({nums[0]}d, {nums[1]}d)" : null, out expr);
                case "System.Windows.Size":
                    return TryParseDoublesExpr(trimmed, static nums => nums.Length == 2 ? $"new global::System.Windows.Size({nums[0]}d, {nums[1]}d)" : null, out expr);
                case "System.Windows.Vector":
                    return TryParseDoublesExpr(trimmed, static nums => nums.Length == 2 ? $"new global::System.Windows.Vector({nums[0]}d, {nums[1]}d)" : null, out expr);
                case "System.Windows.Rect":
                    return TryParseDoublesExpr(trimmed, static nums => nums.Length == 4 ? $"new global::System.Windows.Rect({nums[0]}d, {nums[1]}d, {nums[2]}d, {nums[3]}d)" : null, out expr);
                case "System.Windows.Media.Geometry":
                    expr = $"global::System.Windows.Media.Geometry.Parse({ToCSharpStringLiteral(trimmed)})";
                    return true;
                case "System.Uri":
                    expr = $"new global::System.Uri({ToCSharpStringLiteral(trimmed)}, global::System.UriKind.RelativeOrAbsolute)";
                    return true;
                case "System.TimeSpan":
                    if (TimeSpan.TryParse(trimmed, System.Globalization.CultureInfo.InvariantCulture, out TimeSpan ts))
                    {
                        expr = $"new global::System.TimeSpan({ts.Ticks}L)";
                        return true;
                    }
                    return false;
                case "System.Windows.Duration":
                    if (string.Equals(trimmed, "Automatic", StringComparison.OrdinalIgnoreCase))
                    {
                        expr = "global::System.Windows.Duration.Automatic";
                        return true;
                    }
                    if (string.Equals(trimmed, "Forever", StringComparison.OrdinalIgnoreCase))
                    {
                        expr = "global::System.Windows.Duration.Forever";
                        return true;
                    }
                    if (TimeSpan.TryParse(trimmed, System.Globalization.CultureInfo.InvariantCulture, out TimeSpan dts))
                    {
                        expr = $"new global::System.Windows.Duration(new global::System.TimeSpan({dts.Ticks}L))";
                        return true;
                    }
                    return false;
            }

            return false;
        }

        private static bool TryParseColorExpr(string trimmed, out string colorExpr)
        {
            colorExpr = string.Empty;
            if (trimmed.StartsWith("#", StringComparison.Ordinal))
            {
                string hex = trimmed.Substring(1);
                if (hex.Length == 3 &&
                    TryHexByte(hex[0], hex[0], out byte r3) &&
                    TryHexByte(hex[1], hex[1], out byte g3) &&
                    TryHexByte(hex[2], hex[2], out byte b3))
                {
                    colorExpr = $"global::System.Windows.Media.Color.FromArgb(255, {r3}, {g3}, {b3})";
                    return true;
                }
                if (hex.Length == 4 &&
                    TryHexByte(hex[0], hex[0], out byte a4) &&
                    TryHexByte(hex[1], hex[1], out byte r4) &&
                    TryHexByte(hex[2], hex[2], out byte g4) &&
                    TryHexByte(hex[3], hex[3], out byte b4))
                {
                    colorExpr = $"global::System.Windows.Media.Color.FromArgb({a4}, {r4}, {g4}, {b4})";
                    return true;
                }
                if (hex.Length == 6 &&
                    TryHexByte(hex[0], hex[1], out byte r6) &&
                    TryHexByte(hex[2], hex[3], out byte g6) &&
                    TryHexByte(hex[4], hex[5], out byte b6))
                {
                    colorExpr = $"global::System.Windows.Media.Color.FromArgb(255, {r6}, {g6}, {b6})";
                    return true;
                }
                if (hex.Length == 8 &&
                    TryHexByte(hex[0], hex[1], out byte a8) &&
                    TryHexByte(hex[2], hex[3], out byte r8) &&
                    TryHexByte(hex[4], hex[5], out byte g8) &&
                    TryHexByte(hex[6], hex[7], out byte b8))
                {
                    colorExpr = $"global::System.Windows.Media.Color.FromArgb({a8}, {r8}, {g8}, {b8})";
                    return true;
                }
                return false;
            }

            string? known = MatchKnownColorName(trimmed);
            if (known is not null)
            {
                colorExpr = "global::System.Windows.Media.Colors." + known;
                return true;
            }
            return false;
        }

        private static bool TryHexByte(char hi, char lo, out byte val)
        {
            int h = HexVal(hi);
            int l = HexVal(lo);
            if (h < 0 || l < 0) { val = 0; return false; }
            val = (byte)((h << 4) | l);
            return true;
        }

        private static int HexVal(char c) =>
            c >= '0' && c <= '9' ? c - '0' :
            c >= 'a' && c <= 'f' ? c - 'a' + 10 :
            c >= 'A' && c <= 'F' ? c - 'A' + 10 : -1;

        private static string? MatchStandardFontWeight(string name) => name.ToLowerInvariant() switch
        {
            "thin" => "Thin",
            "extralight" or "ultralight" => "ExtraLight",
            "light" => "Light",
            "normal" or "regular" => "Normal",
            "medium" => "Medium",
            "demibold" or "semibold" => "SemiBold",
            "bold" => "Bold",
            "extrabold" or "ultrabold" => "ExtraBold",
            "black" or "heavy" => "Black",
            "extrablack" or "ultrablack" => "ExtraBlack",
            _ => null
        };

        private static string? MatchKnownColorName(string name) => name.ToLowerInvariant() switch
        {
            "transparent" => "Transparent",
            "black" => "Black",
            "white" => "White",
            "red" => "Red",
            "green" => "Green",
            "blue" => "Blue",
            "yellow" => "Yellow",
            "cyan" => "Cyan",
            "magenta" => "Magenta",
            "gray" => "Gray",
            "darkgray" => "DarkGray",
            "lightgray" => "LightGray",
            "orange" => "Orange",
            "purple" => "Purple",
            "pink" => "Pink",
            "brown" => "Brown",
            "navy" => "Navy",
            "teal" => "Teal",
            "olive" => "Olive",
            "silver" => "Silver",
            "gold" => "Gold",
            _ => null
        };

        private static bool TryParseDoublesExpr(string trimmed, Func<string[], string?> builder, out string expr)
        {
            expr = string.Empty;
            string[] tokens = trimmed.Split(new[] { ',', ' ' }, StringSplitOptions.RemoveEmptyEntries);
            var formatted = new string[tokens.Length];
            for (int i = 0; i < tokens.Length; i++)
            {
                if (!double.TryParse(tokens[i], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out double d))
                {
                    return false;
                }
                formatted[i] = d.ToString("R", System.Globalization.CultureInfo.InvariantCulture);
            }
            string? built = builder(formatted);
            if (built is null) return false;
            expr = built;
            return true;
        }

        private static string ToCSharpStringLiteral(string value)
        {
            var sb = new StringBuilder(value.Length + 4);
            sb.Append('"');
            foreach (char c in value)
            {
                switch (c)
                {
                    case '\\': sb.Append("\\\\"); break;
                    case '"': sb.Append("\\\""); break;
                    case '\r': sb.Append("\\r"); break;
                    case '\n': sb.Append("\\n"); break;
                    case '\t': sb.Append("\\t"); break;
                    default: sb.Append(c); break;
                }
            }
            sb.Append('"');
            return sb.ToString();
        }

        private static string ToCSharpCharLiteral(char c) => c switch
        {
            '\\' => "'\\\\'",
            '\'' => "'\\''",
            '\r' => "'\\r'",
            '\n' => "'\\n'",
            '\t' => "'\\t'",
            _ => $"'{c}'"
        };

        private static void CollectNamedElements(
            XElement element,
            string xamlNs,
            string classNamespace,
            Compilation compilation,
            List<(string Name, string TagName, string FullTypeName)> results)
        {
            foreach (XElement child in element.Elements())
            {
                string localName = child.Name.LocalName;
                if (localName.Contains('.'))
                {
                    if (localName.EndsWith(".Resources", StringComparison.Ordinal) ||
                        localName.EndsWith(".Style", StringComparison.Ordinal) ||
                        localName.EndsWith(".Template", StringComparison.Ordinal))
                    {
                        continue;
                    }
                    CollectNamedElements(child, xamlNs, classNamespace, compilation, results);
                    continue;
                }

                if (localName == "Style" || localName == "ControlTemplate" || localName == "ResourceDictionary")
                {
                    continue;
                }

                string? xName = child.Attribute(XName.Get("Name", xamlNs))?.Value ?? child.Attribute("Name")?.Value;
                if (!string.IsNullOrWhiteSpace(xName))
                {
                    INamedTypeSymbol? sym = ResolveElementType(localName, child.Name.NamespaceName, classNamespace, compilation);
                    string full = sym is not null ? sym.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat) : string.Empty;
                    results.Add((xName!, localName, full));
                }

                CollectNamedElements(child, xamlNs, classNamespace, compilation, results);
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
