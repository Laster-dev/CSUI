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
                    string hash = unchecked((uint)StringComparer.OrdinalIgnoreCase.GetHashCode(xamlFile.Path)).ToString("X8", System.Globalization.CultureInfo.InvariantCulture);
                    spc.AddSource($"{fileName}_{hash}.xaml.g.cs", SourceText.From(generated!, Encoding.UTF8));
                }
            });

            // 4. Whole-project XAML integration: App.Main() for StartupUri, Application.RegisterComponentFactory,
            // standalone ResourceDictionary.RegisterSource, and zero-attribute POCO/INPC {Binding} registration.
            context.RegisterSourceOutput(xamlFiles.Collect().Combine(compilationProvider), static (spc, pair) =>
            {
                ImmutableArray<AdditionalText> allXamls = pair.Left;
                Compilation compilation = pair.Right;
                if (allXamls.IsDefaultOrEmpty)
                {
                    return;
                }

                EmitAero2ThemeOutputs(spc, allXamls, compilation);

                string? bootstrap = TryGenerateProjectXamlBootstrap(allXamls, compilation);
                if (!string.IsNullOrEmpty(bootstrap))
                {
                    spc.AddSource("CsuiApplicationAndBindings.g.cs", SourceText.From(bootstrap!, Encoding.UTF8));
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

            bool hasCodeBehind = compilation.GetTypeByMetadataName(xClass) is not null;
            INamedTypeSymbol? inferredBaseType = hasCodeBehind
                ? null
                : ResolveElementType(root.Name.LocalName, root.Name.NamespaceName, ns, compilation);

            // Try compiling XAML directly into pure strongly-typed C# (zero reflection, zero XamlMarkup root)
            string? compiledBody = TryCompileXamlInitializeBody(root, xClass, ns, XamlNs, compilation, namedElements.Count > 0);

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

            sb.Append(indent).Append("public partial class ").Append(className).Append(" : ");
            if (inferredBaseType is not null)
            {
                sb.Append(inferredBaseType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)).Append(", ");
            }
            sb.AppendLine("global::System.Windows.Markup.IComponentConnector");
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
            sb.Append(memberIndent).AppendLine("void global::System.Windows.Markup.IComponentConnector.Connect(int connectionId, object target) { }");
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
            "System.Windows.Controls.Ribbon",
            "System.Windows.Shapes",
            "System.Windows.Media",
            "System.Windows.Media.Effects",
            "System.Windows.Media.Animation",
            "System.Windows.Media.Imaging",
            "System.Windows.Media.Media3D",
            "System.Windows.Documents",
            "System.Windows.Ink",
            "System.Windows.Navigation",
            "System.Windows.Automation",
            "System.Windows.Markup",
            "System.Windows.Input",
            "System.Windows.Data",
            "System.Windows.Shell",
            "CSUI.Core",
        };

        private sealed class XamlCompileContext
        {
            public XamlCompileContext(Compilation compilation, string classNamespace, string xamlNs, bool rootIsFrameworkElement)
            {
                Compilation = compilation;
                ClassNamespace = classNamespace;
                XamlNs = xamlNs;
                RootIsFrameworkElement = rootIsFrameworkElement;
            }

            public Compilation Compilation { get; }
            public string ClassNamespace { get; }
            public string XamlNs { get; }
            public bool RootIsFrameworkElement { get; }
            public int VarCounter;
            public bool NeedsFindResourceHelper;
            public bool HasInstanceClass { get; set; } = true;
            public List<(string Expr, bool IsDict)> ResourceScopes { get; } = new List<(string Expr, bool IsDict)>();
            public Dictionary<string, (string TypeName, string MethodName)>? ThemeResourceIndex { get; set; }
            public string? CurrentThemeTypeName { get; set; }
            public Dictionary<string, INamedTypeSymbol> TemplateNamedElementTypes { get; } = new Dictionary<string, INamedTypeSymbol>(StringComparer.Ordinal);
            public Stack<HashSet<string>> LocalResourceKeyScopes { get; } = new Stack<HashSet<string>>();
            public string? FailureReason { get; set; }

            public bool Fail(string reason)
            {
                FailureReason ??= reason;
                return false;
            }

            public string NextVar(string prefix = "__csui")
            {
                VarCounter++;
                return prefix + VarCounter.ToString(System.Globalization.CultureInfo.InvariantCulture);
            }
        }

        private static string? TryCompileXamlInitializeBody(
            XElement root,
            string xClass,
            string classNamespace,
            string xamlNs,
            Compilation compilation,
            bool hasNamedElements)
        {
            INamedTypeSymbol? rootType = compilation.GetTypeByMetadataName(xClass)
                ?? ResolveElementType(root.Name.LocalName, root.Name.NamespaceName, classNamespace, compilation);
            if (rootType is null)
            {
                return null;
            }

            bool isRootFe = InheritsFrom(rootType, "System.Windows.FrameworkElement");
            var ctx = new XamlCompileContext(compilation, classNamespace, xamlNs, isRootFe);
            if (isRootFe)
            {
                ctx.ResourceScopes.Add(("this", false));
            }
            else if (InheritsFrom(rootType, "System.Windows.Application"))
            {
                ctx.ResourceScopes.Add(("this.Resources", true));
            }

            var body = new StringBuilder();
            if (hasNamedElements && isRootFe)
            {
                body.AppendLine("if (global::System.Windows.NameScope.GetNameScope(this) is null)");
                body.AppendLine("{");
                body.AppendLine("    global::System.Windows.NameScope.SetNameScope(this, new global::System.Windows.NameScope());");
                body.AppendLine("}");
            }

            var bindingSb = new StringBuilder();
            if (!TryEmitElementPopulation(body, bindingSb, root, "this", rootType, isRoot: true, ctx))
            {
                return null;
            }

            if (bindingSb.Length > 0)
            {
                body.Append(bindingSb);
            }

            if (ctx.NeedsFindResourceHelper)
            {
                body.AppendLine();
                body.AppendLine("static T __CsuiFindRes<T>(object key, params object?[] scopes)");
                body.AppendLine("{");
                body.AppendLine("    foreach (object? s in scopes)");
                body.AppendLine("    {");
                body.AppendLine("        object? found = s switch");
                body.AppendLine("        {");
                body.AppendLine("            global::System.Windows.FrameworkElement fe => fe.TryFindResource(key),");
                body.AppendLine("            global::System.Windows.ResourceDictionary rd => rd[key],");
                body.AppendLine("            _ => null");
                body.AppendLine("        };");
                body.AppendLine("        if (found is T typed) return typed;");
                body.AppendLine("    }");
                body.AppendLine("    object? g = global::System.Windows.Application.Current?.TryFindResource(key);");
                body.AppendLine("    if (g is T gt) return gt;");
                body.AppendLine("    throw new global::System.InvalidOperationException($\"StaticResource '{key}' was not found.\");");
                body.AppendLine("}");
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
            XamlCompileContext ctx)
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

                if (attrNs == ctx.XamlNs)
                {
                    if (attrLocal == "Class" || attrLocal == "ClassModifier" || attrLocal == "FieldModifier" || attrLocal == "Key" || attrLocal == "Shared")
                    {
                        continue;
                    }
                    if (attrLocal == "Name")
                    {
                        if (!isRoot && ctx.HasInstanceClass)
                        {
                            sb.Append("this.").Append(attr.Value).Append(" = ").Append(targetExpr).AppendLine(";");
                        }
                        if (ctx.HasInstanceClass && ctx.RootIsFrameworkElement && InheritsFrom(targetType, "System.Windows.DependencyObject"))
                        {
                            sb.Append("this.RegisterName(").Append(ToCSharpStringLiteral(attr.Value)).Append(", ").Append(targetExpr).AppendLine(");");
                        }
                        if (FindProperty(targetType, "Name") is IPropertySymbol nameProp && nameProp.SetMethod is not null)
                        {
                            sb.Append(targetExpr).Append(".Name = ").Append(ToCSharpStringLiteral(attr.Value)).AppendLine(";");
                        }
                        continue;
                    }
                    return false;
                }

                if (attrNs == "http://schemas.openxmlformats.org/markup-compatibility/2006" ||
                    attrNs == "http://schemas.microsoft.com/expression/blend/2008" ||
                    attrNs == "http://www.w3.org/XML/1998/namespace")
                {
                    continue;
                }

                // Attached property with explicit namespace prefix (e.g. local:MyAttach.Tag="..." or ribbon:KeyTipService.KeyTip="V")
                int dotIdx = attrLocal.IndexOf('.');
                if (!string.IsNullOrEmpty(attrNs))
                {
                    if (dotIdx > 0)
                    {
                        string ownerName = attrLocal.Substring(0, dotIdx);
                        string propName = attrLocal.Substring(dotIdx + 1);
                        INamedTypeSymbol? ownerType = ResolveElementType(ownerName, attrNs, ctx.ClassNamespace, ctx.Compilation);
                        if (ownerType is null || !TryEmitAttachedPropertyAssignment(sb, bindingSb, el, targetExpr, ownerType, propName, attr.Value, ctx))
                        {
                            return false;
                        }
                        continue;
                    }
                    return false;
                }

                if (attrLocal == "Name")
                {
                    if (!isRoot && ctx.HasInstanceClass)
                    {
                        sb.Append("this.").Append(attr.Value).Append(" = ").Append(targetExpr).AppendLine(";");
                    }
                    if (ctx.HasInstanceClass && ctx.RootIsFrameworkElement && InheritsFrom(targetType, "System.Windows.DependencyObject"))
                    {
                        sb.Append("this.RegisterName(").Append(ToCSharpStringLiteral(attr.Value)).Append(", ").Append(targetExpr).AppendLine(");");
                    }
                }

                // Attached property syntax without explicit prefix: Owner.Prop="..."
                if (dotIdx > 0)
                {
                    string ownerName = attrLocal.Substring(0, dotIdx);
                    string propName = attrLocal.Substring(dotIdx + 1);
                    INamedTypeSymbol? ownerType = ResolveElementType(ownerName, el.Name.NamespaceName, ctx.ClassNamespace, ctx.Compilation);
                    if (ownerType is null)
                    {
                        return false;
                    }

                    if (!TryEmitAttachedPropertyAssignment(sb, bindingSb, el, targetExpr, ownerType, propName, attr.Value, ctx))
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
                        if (!TryEmitMarkupExtensionAssignment(targetOut, targetExpr, targetType, prop, trimmed, el, ctx))
                        {
                            return false;
                        }
                    }
                    else
                    {
                        if (!TryConvertLiteralToCSharp(attr.Value, prop.Type, el, ctx.ClassNamespace, ctx.Compilation, out string valueExpr))
                        {
                            return false;
                        }
                        sb.Append(targetExpr).Append('.').Append(prop.Name).Append(" = ").Append(valueExpr).AppendLine(";");
                    }
                    continue;
                }

                // Grid.RowDefinitions="Auto,*,2*" / ColumnDefinitions="100,*" shorthand
                if ((attrLocal == "RowDefinitions" || attrLocal == "ColumnDefinitions") &&
                    FindProperty(targetType, attrLocal) is not null)
                {
                    if (EmitGridDefinitionsShorthand(sb, targetExpr, attrLocal == "RowDefinitions", attr.Value))
                    {
                        continue;
                    }
                }

                if (FindEvent(targetType, attrLocal) is IEventSymbol evt)
                {
                    sb.Append(targetExpr).Append('.').Append(evt.Name).Append(" += this.").Append(attr.Value.Trim()).AppendLine(";");
                    continue;
                }

                return ctx.Fail($"Unknown attr '{attrLocal}' on '{targetType.Name}'");
            }

            // 2. Emit <*.Resources> first so StaticResources inside sibling children resolve regardless of element order
            bool pushedScope = false;
            int scopeInsertIndex = 0;
            foreach (XElement childEl in el.Elements())
            {
                string childLocal = childEl.Name.LocalName;
                int dotIdx = childLocal.IndexOf('.');
                if (dotIdx > 0 && childLocal.Substring(dotIdx + 1) == "Resources")
                {
                    string resExpr = targetExpr + ".Resources";
                    if (!TryEmitResourcesCollection(sb, bindingSb, resExpr, childEl, ctx))
                    {
                        return false;
                    }
                    if (!isRoot && !pushedScope && InheritsFrom(targetType, "System.Windows.FrameworkElement"))
                    {
                        ctx.ResourceScopes.Insert(scopeInsertIndex, (targetExpr, false));
                        pushedScope = true;
                    }
                }
            }

            // 3. Emit remaining child property elements and content elements
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
                        string ownerName = childLocal.Substring(0, dotIdx);
                        string propName = childLocal.Substring(dotIdx + 1);
                        if (propName == "Resources")
                        {
                            continue; // Already emitted in step 2
                        }
                        if (propName == "Triggers")
                        {
                            foreach (XElement trigEl in childEl.Elements())
                            {
                                if (!TryEmitTriggerToCollection(sb, targetExpr + ".Triggers", trigEl, targetType, ctx))
                                {
                                    if (pushedScope) ctx.ResourceScopes.RemoveAt(scopeInsertIndex);
                                    return false;
                                }
                            }
                            continue;
                        }

                        IPropertySymbol? prop = FindProperty(targetType, propName);
                        if (prop is null)
                        {
                            INamedTypeSymbol? ownerType = ResolveElementType(ownerName, childEl.Name.NamespaceName, ctx.ClassNamespace, ctx.Compilation);
                            if (ownerType is not null && childEl.Elements().Count() == 1)
                            {
                                XElement valEl = childEl.Elements().First();
                                if (TryEmitAnyValueElement(sb, bindingSb, valEl, ctx, out string valVar))
                                {
                                    sb.Append(ownerType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat))
                                      .Append(".Set").Append(propName).Append('(').Append(targetExpr).Append(", ").Append(valVar).AppendLine(");");
                                    continue;
                                }
                            }
                            if (pushedScope) ctx.ResourceScopes.RemoveAt(scopeInsertIndex);
                            return false;
                        }

                        bool isCollection = IsCollectionLike(prop.Type);
                        foreach (XElement itemEl in childEl.Elements())
                        {
                            INamedTypeSymbol? itemResolved = ResolveElementType(itemEl.Name.LocalName, itemEl.Name.NamespaceName, ctx.ClassNamespace, ctx.Compilation);
                            if (isCollection && itemResolved is not null && SymbolEqualityComparer.Default.Equals(itemResolved, prop.Type))
                            {
                                foreach (XElement subItem in itemEl.Elements())
                                {
                                    if (!TryEmitAnyValueElement(sb, bindingSb, subItem, ctx, out string subVar))
                                    {
                                        if (pushedScope) ctx.ResourceScopes.RemoveAt(scopeInsertIndex);
                                        return false;
                                    }
                                    sb.Append(targetExpr).Append('.').Append(prop.Name).Append(".Add(").Append(subVar).AppendLine(");");
                                }
                                continue;
                            }

                            if (!TryEmitAnyValueElement(sb, bindingSb, itemEl, ctx, out string childVar))
                            {
                                if (pushedScope) ctx.ResourceScopes.RemoveAt(scopeInsertIndex);
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
                                    if (pushedScope) ctx.ResourceScopes.RemoveAt(scopeInsertIndex);
                                    return false;
                                }
                                sb.Append(targetExpr).Append('.').Append(prop.Name).Append(" = ").Append(childVar).AppendLine(";");
                            }
                        }
                        continue;
                    }

                    if (!TryEmitAnyValueElement(sb, bindingSb, childEl, ctx, out string childVarName))
                    {
                        if (pushedScope) ctx.ResourceScopes.RemoveAt(scopeInsertIndex);
                        return false;
                    }

                    if (!TryAddContentChild(sb, targetExpr, targetType, childVarName, ctx.Compilation))
                    {
                        if (pushedScope) ctx.ResourceScopes.RemoveAt(scopeInsertIndex);
                        return ctx.Fail($"TryAddContentChild '{childEl.Name.LocalName}' into '{targetType.Name}' failed");
                    }
                }
            }

            if (pushedScope)
            {
                ctx.ResourceScopes.RemoveAt(scopeInsertIndex);
            }

            if (!hasElementChildren)
            {
                string innerText = string.Concat(el.Nodes().OfType<XText>().Select(static t => t.Value));
                if (!string.IsNullOrWhiteSpace(innerText))
                {
                    if (!TryAssignContentText(sb, targetExpr, targetType, innerText.Trim(), el, ctx))
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        private static bool TryEmitAnyValueElement(
            StringBuilder sb,
            StringBuilder bindingSb,
            XElement valEl,
            XamlCompileContext ctx,
            out string varName)
        {
            string local = valEl.Name.LocalName;
            string ns = valEl.Name.NamespaceName;

            if (ns == ctx.XamlNs || ns == "clr-namespace:System;assembly=mscorlib" || ns == "clr-namespace:System;assembly=System.Runtime")
            {
                if (local == "Array" || local == "ArrayExtension")
                {
                    string elemTypeName = "object";
                    string? typeRaw = valEl.Attribute("Type")?.Value;
                    if (!string.IsNullOrWhiteSpace(typeRaw))
                    {
                        if (!TryResolveTypeAttributeExpr(typeRaw!, valEl, ctx, out INamedTypeSymbol? elemSym, out _) || elemSym is null)
                        {
                            varName = "";
                            return false;
                        }
                        elemTypeName = elemSym.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
                    }

                    var itemVars = new List<string>();
                    foreach (XElement child in valEl.Elements())
                    {
                        if (child.Name.LocalName == "Array.Items" || child.Name.LocalName == "ArrayExtension.Items")
                        {
                            foreach (XElement innerItem in child.Elements())
                            {
                                if (!TryEmitAnyValueElement(sb, bindingSb, innerItem, ctx, out string itemVar))
                                {
                                    varName = "";
                                    return false;
                                }
                                itemVars.Add(itemVar);
                            }
                        }
                        else
                        {
                            if (!TryEmitAnyValueElement(sb, bindingSb, child, ctx, out string itemVar))
                            {
                                varName = "";
                                return false;
                            }
                            itemVars.Add(itemVar);
                        }
                    }

                    varName = ctx.NextVar("__arr");
                    sb.Append("var ").Append(varName).Append(" = new ").Append(elemTypeName).Append("[] { ");
                    for (int i = 0; i < itemVars.Count; i++)
                    {
                        if (i > 0) sb.Append(", ");
                        sb.Append(itemVars[i]);
                    }
                    sb.AppendLine(" };");
                    return true;
                }
                if (local == "String")
                {
                    varName = ctx.NextVar("__str");
                    sb.Append("var ").Append(varName).Append(" = ").Append(ToCSharpStringLiteral(valEl.Value)).AppendLine(";");
                    return true;
                }
                if (local == "Double" && double.TryParse(valEl.Value.Trim(), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out double dVal))
                {
                    varName = ctx.NextVar("__dbl");
                    sb.Append("double ").Append(varName).Append(" = ").Append(dVal.ToString("R", System.Globalization.CultureInfo.InvariantCulture)).AppendLine("d;");
                    return true;
                }
                if (local == "Int32" && int.TryParse(valEl.Value.Trim(), System.Globalization.NumberStyles.Integer, System.Globalization.CultureInfo.InvariantCulture, out int iVal))
                {
                    varName = ctx.NextVar("__int");
                    sb.Append("int ").Append(varName).Append(" = ").Append(iVal.ToString(System.Globalization.CultureInfo.InvariantCulture)).AppendLine(";");
                    return true;
                }
                if (local == "Boolean" && bool.TryParse(valEl.Value.Trim(), out bool bVal))
                {
                    varName = ctx.NextVar("__bool");
                    sb.Append("bool ").Append(varName).Append(" = ").Append(bVal ? "true" : "false").AppendLine(";");
                    return true;
                }
            }

            if (local == "Style")
            {
                return TryEmitStyleElement(sb, valEl, ctx, out varName);
            }
            if (local == "ControlTemplate")
            {
                return TryEmitControlTemplateElement(sb, valEl, inheritedTargetType: null, ctx, out varName);
            }
            if (local == "DataTemplate")
            {
                return TryEmitDataTemplateElement(sb, valEl, ctx, out varName);
            }
            if (local == "ItemsPanelTemplate")
            {
                return TryEmitItemsPanelTemplateElement(sb, valEl, ctx, out varName);
            }
            if (local == "Thickness")
            {
                if (!TryConvertLiteralToCSharp(valEl.Value, ctx.Compilation.GetTypeByMetadataName("System.Windows.Thickness")!, valEl, ctx.ClassNamespace, ctx.Compilation, out string thExpr))
                {
                    varName = string.Empty;
                    return false;
                }
                varName = ctx.NextVar("__th");
                sb.Append("var ").Append(varName).Append(" = ").Append(thExpr).AppendLine(";");
                return true;
            }
            if (local == "Visibility")
            {
                if (!TryConvertLiteralToCSharp(valEl.Value, ctx.Compilation.GetTypeByMetadataName("System.Windows.Visibility")!, valEl, ctx.ClassNamespace, ctx.Compilation, out string visExpr))
                {
                    varName = string.Empty;
                    return false;
                }
                varName = ctx.NextVar("__vis");
                sb.Append("var ").Append(varName).Append(" = ").Append(visExpr).AppendLine(";");
                return true;
            }
            if (local == "Geometry")
            {
                varName = ctx.NextVar("__geo");
                sb.Append("var ").Append(varName).Append(" = global::System.Windows.Media.Geometry.Parse(")
                  .Append(ToCSharpStringLiteral(valEl.Value.Trim())).AppendLine(");");
                return true;
            }
            if (local == "ResourceDictionary")
            {
                varName = ctx.NextVar("__rd");
                sb.Append("var ").Append(varName).AppendLine(" = new global::System.Windows.ResourceDictionary();");
                XAttribute? sourceAttr = valEl.Attribute("Source");
                if (sourceAttr is not null)
                {
                    sb.Append(varName).Append(".Source = new global::System.Uri(")
                      .Append(ToCSharpStringLiteral(sourceAttr.Value))
                      .AppendLine(", global::System.UriKind.RelativeOrAbsolute);");
                }
                return TryEmitResourcesCollection(sb, bindingSb, varName, valEl, ctx);
            }

            return TryCreateAndEmitChildElement(sb, bindingSb, valEl, ctx, out varName, out _);
        }

        private static bool EmitGridDefinitionsShorthand(StringBuilder sb, string targetExpr, bool isRow, string raw)
        {
            string[] tokens = raw.Split(new[] { ',', ' ' }, StringSplitOptions.RemoveEmptyEntries);
            if (tokens.Length == 0)
            {
                return false;
            }

            foreach (string tok in tokens)
            {
                if (!TryConvertGridLengthExpr(tok.Trim(), out string glExpr))
                {
                    return false;
                }

                if (isRow)
                {
                    sb.Append(targetExpr).Append(".RowDefinitions.Add(new global::System.Windows.Controls.RowDefinition { Height = ")
                      .Append(glExpr).AppendLine(" });");
                }
                else
                {
                    sb.Append(targetExpr).Append(".ColumnDefinitions.Add(new global::System.Windows.Controls.ColumnDefinition { Width = ")
                      .Append(glExpr).AppendLine(" });");
                }
            }
            return true;
        }

        private static bool TryConvertGridLengthExpr(string trimmed, out string expr)
        {
            expr = string.Empty;
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
        }

        private static bool TryEmitResourcesCollection(
            StringBuilder sb,
            StringBuilder bindingSb,
            string dictExpr,
            XElement containerEl,
            XamlCompileContext ctx)
        {
            var localKeys = new HashSet<string>(StringComparer.Ordinal);
            foreach (XElement kEl in containerEl.Elements())
            {
                string? rawK = kEl.Attribute(XName.Get("Key", ctx.XamlNs))?.Value;
                if (!string.IsNullOrWhiteSpace(rawK))
                {
                    localKeys.Add(rawK!);
                    localKeys.Add(NormalizeThemeResourceKey(rawK!));
                }
            }
            ctx.LocalResourceKeyScopes.Push(localKeys);
            ctx.ResourceScopes.Insert(0, (dictExpr, true));
            try
            {
                foreach (XElement child in containerEl.Elements())
                {
                    string local = child.Name.LocalName;
                    if (local == "ResourceDictionary")
                    {
                        XAttribute? srcAttr = child.Attribute("Source");
                        if (srcAttr is not null && !child.HasElements)
                        {
                            sb.Append(dictExpr).Append(".Source = new global::System.Uri(")
                              .Append(ToCSharpStringLiteral(srcAttr.Value))
                              .AppendLine(", global::System.UriKind.RelativeOrAbsolute);");
                            continue;
                        }
                        if (!TryEmitResourcesCollection(sb, bindingSb, dictExpr, child, ctx))
                        {
                            return false;
                        }
                        continue;
                    }

                    if (local == "ResourceDictionary.MergedDictionaries")
                    {
                        foreach (XElement mergedEl in child.Elements())
                        {
                            if (!TryEmitAnyValueElement(sb, bindingSb, mergedEl, ctx, out string mergedVar))
                            {
                                return false;
                            }
                            sb.Append(dictExpr).Append(".MergedDictionaries.Add(").Append(mergedVar).AppendLine(");");
                        }
                        continue;
                    }

                    if (!TryEmitSingleResourceEntry(sb, bindingSb, dictExpr, child, ctx))
                    {
                        return false;
                    }
                }
                return true;
            }
            finally
            {
                ctx.ResourceScopes.RemoveAt(0);
                ctx.LocalResourceKeyScopes.Pop();
            }
        }

        private static bool TryEmitSingleResourceEntry(
            StringBuilder sb,
            StringBuilder bindingSb,
            string dictExpr,
            XElement entryEl,
            XamlCompileContext ctx)
        {
            string? rawKey = entryEl.Attribute(XName.Get("Key", ctx.XamlNs))?.Value;
            string? keyExpr = null;

            if (!string.IsNullOrWhiteSpace(rawKey))
            {
                if (!TryResolveMarkupOrLiteralKeyExpr(rawKey!, entryEl, ctx, out keyExpr))
                {
                    return false;
                }
            }
            else if (entryEl.Name.LocalName == "Style")
            {
                string? targetTypeRaw = entryEl.Attribute("TargetType")?.Value;
                if (string.IsNullOrWhiteSpace(targetTypeRaw) ||
                    !TryResolveTypeAttributeExpr(targetTypeRaw!, entryEl, ctx, out INamedTypeSymbol? targetSym, out string typeofExpr))
                {
                    return false;
                }
                _ = targetSym;
                keyExpr = typeofExpr;
            }
            else if (entryEl.Name.LocalName == "DataTemplate")
            {
                string? dataTypeRaw = entryEl.Attribute("DataType")?.Value;
                if (string.IsNullOrWhiteSpace(dataTypeRaw) ||
                    !TryResolveTypeAttributeExpr(dataTypeRaw!, entryEl, ctx, out _, out string typeofExpr))
                {
                    return false;
                }
                keyExpr = $"new global::System.Windows.DataTemplateKey({typeofExpr})";
            }
            else
            {
                return false;
            }

            string? sharedAttr = entryEl.Attribute(XName.Get("Shared", ctx.XamlNs))?.Value;
            if (string.Equals(sharedAttr?.Trim(), "False", StringComparison.OrdinalIgnoreCase))
            {
                var nonSharedSb = new StringBuilder();
                var nonSharedBindingSb = new StringBuilder();
                if (!TryEmitAnyValueElement(nonSharedSb, nonSharedBindingSb, entryEl, ctx, out string valVar))
                {
                    return false;
                }

                sb.Append(dictExpr).Append('[').Append(keyExpr).Append("] = global::System.Windows.ResourceDictionary.CreateNonShared(() => {").AppendLine();
                sb.Append(nonSharedSb);
                if (nonSharedBindingSb.Length > 0)
                {
                    sb.Append(nonSharedBindingSb);
                }
                sb.Append("return ").Append(valVar).AppendLine("; });");
                return true;
            }

            if (!TryEmitAnyValueElement(sb, bindingSb, entryEl, ctx, out string sharedValVar))
            {
                return false;
            }

            sb.Append(dictExpr).Append('[').Append(keyExpr).Append("] = ").Append(sharedValVar).AppendLine(";");
            return true;
        }

        private static bool TryResolveMarkupOrLiteralKeyExpr(string rawKey, XElement el, XamlCompileContext ctx, out string keyExpr)
        {
            string trimmed = rawKey.Trim();
            if (trimmed.StartsWith("{", StringComparison.Ordinal) && trimmed.EndsWith("}", StringComparison.Ordinal))
            {
                return TryResolveMarkupValueExpr(trimmed, el, null, ctx, out keyExpr);
            }
            keyExpr = ToCSharpStringLiteral(rawKey);
            return true;
        }

        private static bool TryResolveTypeAttributeExpr(
            string raw,
            XElement el,
            XamlCompileContext ctx,
            out INamedTypeSymbol? typeSym,
            out string typeofExpr)
        {
            typeSym = null;
            typeofExpr = string.Empty;
            string trimmed = raw.Trim();
            if (trimmed.StartsWith("{", StringComparison.Ordinal) && trimmed.EndsWith("}", StringComparison.Ordinal))
            {
                string inner = trimmed.Substring(1, trimmed.Length - 2).Trim();
                if (inner.StartsWith("x:Type ", StringComparison.Ordinal))
                {
                    trimmed = inner.Substring("x:Type ".Length).Trim();
                }
                else
                {
                    return false;
                }
            }

            typeSym = ResolveMarkupType(trimmed, el, ctx);
            if (typeSym is null)
            {
                return false;
            }

            typeofExpr = "typeof(" + typeSym.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat) + ")";
            return true;
        }

        private static bool TryEmitStyleElement(
            StringBuilder sb,
            XElement styleEl,
            XamlCompileContext ctx,
            out string styleVar)
        {
            styleVar = ctx.NextVar("__style");
            INamedTypeSymbol? styleTargetType = null;
            string? targetTypeAttr = styleEl.Attribute("TargetType")?.Value;
            if (!string.IsNullOrWhiteSpace(targetTypeAttr))
            {
                if (!TryResolveTypeAttributeExpr(targetTypeAttr!, styleEl, ctx, out styleTargetType, out string typeofExpr))
                {
                    return false;
                }
                sb.Append("var ").Append(styleVar).Append(" = new global::System.Windows.Style(").Append(typeofExpr).AppendLine(");");
            }
            else
            {
                sb.Append("var ").Append(styleVar).AppendLine(" = new global::System.Windows.Style();");
            }

            XAttribute? basedOn = styleEl.Attribute("BasedOn");
            if (basedOn is not null)
            {
                string trimmed = basedOn.Value.Trim();
                if (!TryResolveMarkupValueExpr(trimmed, styleEl, ctx.Compilation.GetTypeByMetadataName("System.Windows.Style"), ctx, out string basedOnExpr))
                {
                    return false;
                }
                sb.Append(styleVar).Append(".BasedOn = ").Append(basedOnExpr).AppendLine(";");
            }

            bool pushedStyleScope = false;
            try
            {
                foreach (XElement child in styleEl.Elements())
                {
                    if (child.Name.LocalName == "Style.Resources")
                    {
                        var dummy = new StringBuilder();
                        if (!TryEmitResourcesCollection(sb, dummy, styleVar + ".Resources", child, ctx) || dummy.Length > 0)
                        {
                            return false;
                        }
                        var styleResKeys = new HashSet<string>(StringComparer.Ordinal);
                        foreach (XElement kEl in child.Elements())
                        {
                            string? rawK = kEl.Attribute(XName.Get("Key", ctx.XamlNs))?.Value;
                            if (!string.IsNullOrWhiteSpace(rawK))
                            {
                                styleResKeys.Add(rawK!);
                                styleResKeys.Add(NormalizeThemeResourceKey(rawK!));
                            }
                        }
                        ctx.LocalResourceKeyScopes.Push(styleResKeys);
                        ctx.ResourceScopes.Insert(0, (styleVar + ".Resources", true));
                        pushedStyleScope = true;
                    }
                }

                foreach (XElement child in styleEl.Elements())
                {
                    string local = child.Name.LocalName;
                    if (local == "Style.Resources")
                    {
                        continue;
                    }
                    if (local == "Setter")
                    {
                        if (!TryEmitSetterToCollection(sb, styleVar + ".Setters", child, styleTargetType, allowTargetName: false, ctx))
                        {
                            return false;
                        }
                    }
                    else if (local == "Style.Setters")
                    {
                        foreach (XElement sEl in child.Elements())
                        {
                            if (sEl.Name.LocalName != "Setter" ||
                                !TryEmitSetterToCollection(sb, styleVar + ".Setters", sEl, styleTargetType, allowTargetName: false, ctx))
                            {
                                return false;
                            }
                        }
                    }
                    else if (local == "Style.Triggers")
                    {
                        foreach (XElement trigEl in child.Elements())
                        {
                            if (!TryEmitTriggerToCollection(sb, styleVar + ".Triggers", trigEl, styleTargetType, ctx))
                            {
                                return false;
                            }
                        }
                    }
                    else
                    {
                        return false;
                    }
                }

                return true;
            }
            finally
            {
                if (pushedStyleScope)
                {
                    ctx.ResourceScopes.RemoveAt(0);
                    ctx.LocalResourceKeyScopes.Pop();
                }
            }
        }

        private static bool TryEmitTriggerToCollection(
            StringBuilder sb,
            string triggersExpr,
            XElement trigEl,
            INamedTypeSymbol? defaultTargetType,
            XamlCompileContext ctx)
        {
            string local = trigEl.Name.LocalName;
            if (local == "Trigger")
            {
                string? propAttr = trigEl.Attribute("Property")?.Value;
                string? valAttr = trigEl.Attribute("Value")?.Value;
                string? sourceName = trigEl.Attribute("SourceName")?.Value;
                if (string.IsNullOrWhiteSpace(propAttr) || valAttr is null)
                {
                    return false;
                }

                INamedTypeSymbol? effectiveOwner = (!string.IsNullOrEmpty(sourceName) && ctx.TemplateNamedElementTypes.TryGetValue(sourceName!, out var snType))
                    ? snType
                    : defaultTargetType;

                if (!TryResolveDependencyPropertyRef(propAttr!, effectiveOwner, trigEl, ctx, out string dpExpr, out ITypeSymbol? propClrType))
                {
                    return false;
                }

                ITypeSymbol valueType = propClrType ?? ctx.Compilation.GetSpecialType(SpecialType.System_Object);
                if (!TryResolveAttributeValueExpr(valAttr, valueType, trigEl, ctx, out string valExpr))
                {
                    return false;
                }

                string trigVar = ctx.NextVar("__trig");
                sb.Append("var ").Append(trigVar).Append(" = new global::System.Windows.Trigger { Property = ")
                  .Append(dpExpr).Append(", Value = ").Append(valExpr);
                if (!string.IsNullOrEmpty(sourceName))
                {
                    sb.Append(", SourceName = ").Append(ToCSharpStringLiteral(sourceName!));
                }
                sb.AppendLine(" };");

                if (!TryEmitTriggerBodyChildren(sb, trigVar, trigEl, defaultTargetType, allowTargetName: true, ctx))
                {
                    return false;
                }

                sb.Append(triggersExpr).Append(".Add(").Append(trigVar).AppendLine(");");
                return true;
            }

            if (local == "MultiTrigger")
            {
                string trigVar = ctx.NextVar("__mtrig");
                sb.Append("var ").Append(trigVar).AppendLine(" = new global::System.Windows.MultiTrigger();");
                foreach (XElement child in trigEl.Elements())
                {
                    string cl = child.Name.LocalName;
                    if (cl == "MultiTrigger.Conditions")
                    {
                        foreach (XElement condEl in child.Elements())
                        {
                            if (!TryEmitPropertyCondition(sb, trigVar + ".Conditions", condEl, defaultTargetType, ctx))
                            {
                                return false;
                            }
                        }
                    }
                    else if (cl == "Setter")
                    {
                        if (!TryEmitSetterToCollection(sb, trigVar + ".Setters", child, defaultTargetType, allowTargetName: true, ctx))
                        {
                            return false;
                        }
                    }
                    else if (cl == "MultiTrigger.Setters")
                    {
                        foreach (XElement s in child.Elements())
                        {
                            if (s.Name.LocalName != "Setter" ||
                                !TryEmitSetterToCollection(sb, trigVar + ".Setters", s, defaultTargetType, allowTargetName: true, ctx))
                            {
                                return false;
                            }
                        }
                    }
                    else
                    {
                        return false;
                    }
                }
                sb.Append(triggersExpr).Append(".Add(").Append(trigVar).AppendLine(");");
                return true;
            }

            if (local == "DataTrigger")
            {
                if (!TryEmitBindingFromAttrOrChild(sb, trigEl, "Binding", "DataTrigger.Binding", ctx, out string bindingExpr))
                {
                    return false;
                }
                string? valAttr = trigEl.Attribute("Value")?.Value;
                if (valAttr is null)
                {
                    return false;
                }
                ITypeSymbol valueType = TryInferBindingValueType(trigEl, "Binding", "DataTrigger.Binding", defaultTargetType, ctx)
                    ?? ctx.Compilation.GetSpecialType(SpecialType.System_Object);
                if (!TryResolveAttributeValueExpr(valAttr, valueType, trigEl, ctx, out string valExpr))
                {
                    return false;
                }

                string trigVar = ctx.NextVar("__dtrig");
                sb.Append("var ").Append(trigVar).Append(" = new global::System.Windows.DataTrigger { Binding = ")
                  .Append(bindingExpr).Append(", Value = ").Append(valExpr).AppendLine(" };");

                if (!TryEmitTriggerBodyChildren(sb, trigVar, trigEl, defaultTargetType, allowTargetName: true, ctx, ignoreChildElement: "DataTrigger.Binding"))
                {
                    return false;
                }
                sb.Append(triggersExpr).Append(".Add(").Append(trigVar).AppendLine(");");
                return true;
            }

            if (local == "MultiDataTrigger")
            {
                string trigVar = ctx.NextVar("__mdtrig");
                sb.Append("var ").Append(trigVar).AppendLine(" = new global::System.Windows.MultiDataTrigger();");
                foreach (XElement child in trigEl.Elements())
                {
                    string cl = child.Name.LocalName;
                    if (cl == "MultiDataTrigger.Conditions")
                    {
                        foreach (XElement condEl in child.Elements())
                        {
                            if (!TryEmitBindingCondition(sb, trigVar + ".Conditions", condEl, defaultTargetType, ctx))
                            {
                                return false;
                            }
                        }
                    }
                    else if (cl == "Setter")
                    {
                        if (!TryEmitSetterToCollection(sb, trigVar + ".Setters", child, defaultTargetType, allowTargetName: true, ctx))
                        {
                            return false;
                        }
                    }
                    else if (cl == "MultiDataTrigger.Setters")
                    {
                        foreach (XElement s in child.Elements())
                        {
                            if (s.Name.LocalName != "Setter" ||
                                !TryEmitSetterToCollection(sb, trigVar + ".Setters", s, defaultTargetType, allowTargetName: true, ctx))
                            {
                                return false;
                            }
                        }
                    }
                    else
                    {
                        return false;
                    }
                }
                sb.Append(triggersExpr).Append(".Add(").Append(trigVar).AppendLine(");");
                return true;
            }

            if (local == "EventTrigger")
            {
                string? routedEventRaw = trigEl.Attribute("RoutedEvent")?.Value;
                if (string.IsNullOrWhiteSpace(routedEventRaw) ||
                    !TryResolveRoutedEventExpr(routedEventRaw!, trigEl, defaultTargetType, ctx, out string routedEventExpr))
                {
                    return false;
                }

                string trigVar = ctx.NextVar("__etrig");
                sb.Append("var ").Append(trigVar).Append(" = new global::System.Windows.EventTrigger(").Append(routedEventExpr).AppendLine(");");
                string? sourceName = trigEl.Attribute("SourceName")?.Value;
                if (!string.IsNullOrEmpty(sourceName))
                {
                    sb.Append(trigVar).Append(".SourceName = ").Append(ToCSharpStringLiteral(sourceName!)).AppendLine(";");
                }

                var dummyBindingSb = new StringBuilder();
                foreach (XElement child in trigEl.Elements())
                {
                    if (child.Name.LocalName == "EventTrigger.Actions")
                    {
                        foreach (XElement actionEl in child.Elements())
                        {
                            if (!TryEmitAnyValueElement(sb, dummyBindingSb, actionEl, ctx, out string actVar))
                            {
                                return false;
                            }
                            sb.Append(trigVar).Append(".Actions.Add(").Append(actVar).AppendLine(");");
                        }
                    }
                    else
                    {
                        if (!TryEmitAnyValueElement(sb, dummyBindingSb, child, ctx, out string actVar))
                        {
                            return false;
                        }
                        sb.Append(trigVar).Append(".Actions.Add(").Append(actVar).AppendLine(");");
                    }
                }
                if (dummyBindingSb.Length > 0) sb.Append(dummyBindingSb);
                sb.Append(triggersExpr).Append(".Add(").Append(trigVar).AppendLine(");");
                return true;
            }

            return false;
        }

        private static bool TryResolveRoutedEventExpr(
            string raw,
            XElement el,
            INamedTypeSymbol? defaultTargetType,
            XamlCompileContext ctx,
            out string routedEventExpr)
        {
            routedEventExpr = string.Empty;
            string trimmed = raw.Trim();
            int dot = trimmed.IndexOf('.');
            if (dot > 0)
            {
                string ownerPart = trimmed.Substring(0, dot).Trim();
                string eventName = trimmed.Substring(dot + 1).Trim();
                INamedTypeSymbol? ownerType = ResolveMarkupType(ownerPart, el, ctx);
                if (ownerType is null) return false;
                ISymbol? field = FindStaticRoutedEventField(ownerType, eventName + "Event");
                if (field is null) return false;
                routedEventExpr = field.ContainingType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat) + "." + field.Name;
                return true;
            }

            if (defaultTargetType is not null)
            {
                ISymbol? field = FindStaticRoutedEventField(defaultTargetType, trimmed + "Event");
                if (field is not null)
                {
                    routedEventExpr = field.ContainingType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat) + "." + field.Name;
                    return true;
                }
            }

            foreach (string fallbackType in new[]
            {
                "System.Windows.FrameworkElement",
                "System.Windows.UIElement",
                "System.Windows.Controls.Primitives.ButtonBase",
                "System.Windows.Controls.Control"
            })
            {
                INamedTypeSymbol? sym = ctx.Compilation.GetTypeByMetadataName(fallbackType);
                if (sym is null) continue;
                ISymbol? field = FindStaticRoutedEventField(sym, trimmed + "Event");
                if (field is not null)
                {
                    routedEventExpr = field.ContainingType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat) + "." + field.Name;
                    return true;
                }
            }

            return false;
        }

        private static ISymbol? FindStaticRoutedEventField(INamedTypeSymbol type, string fieldName)
        {
            for (INamedTypeSymbol? cur = type; cur is not null; cur = cur.BaseType)
            {
                foreach (ISymbol m in cur.GetMembers(fieldName))
                {
                    if (m is IFieldSymbol f && f.IsStatic)
                    {
                        return f;
                    }
                }
            }
            return null;
        }

        private static bool TryEmitTriggerBodyChildren(
            StringBuilder sb,
            string trigVar,
            XElement trigEl,
            INamedTypeSymbol? defaultTargetType,
            bool allowTargetName,
            XamlCompileContext ctx,
            string? ignoreChildElement = null)
        {
            foreach (XElement child in trigEl.Elements())
            {
                string cl = child.Name.LocalName;
                if (ignoreChildElement is not null && cl == ignoreChildElement)
                {
                    continue;
                }
                if (cl == "Setter")
                {
                    if (!TryEmitSetterToCollection(sb, trigVar + ".Setters", child, defaultTargetType, allowTargetName, ctx))
                    {
                        return ctx.Fail($"Trigger setter on '{child.Attribute("Property")?.Value}' failed");
                    }
                }
                else if (cl.EndsWith(".Setters", StringComparison.Ordinal))
                {
                    foreach (XElement s in child.Elements())
                    {
                        if (s.Name.LocalName != "Setter" ||
                            !TryEmitSetterToCollection(sb, trigVar + ".Setters", s, defaultTargetType, allowTargetName, ctx))
                        {
                            return false;
                        }
                    }
                }
                else if (cl.EndsWith(".EnterActions", StringComparison.Ordinal) || cl.EndsWith(".ExitActions", StringComparison.Ordinal))
                {
                    string collProp = cl.EndsWith(".EnterActions", StringComparison.Ordinal) ? ".EnterActions" : ".ExitActions";
                    var dummyBindingSb = new StringBuilder();
                    foreach (XElement actionEl in child.Elements())
                    {
                        if (!TryEmitAnyValueElement(sb, dummyBindingSb, actionEl, ctx, out string actVar))
                        {
                            return false;
                        }
                        sb.Append(trigVar).Append(collProp).Append(".Add(").Append(actVar).AppendLine(");");
                    }
                    if (dummyBindingSb.Length > 0) sb.Append(dummyBindingSb);
                }
                else
                {
                    return false;
                }
            }
            return true;
        }

        private static bool TryEmitPropertyCondition(
            StringBuilder sb,
            string condsExpr,
            XElement condEl,
            INamedTypeSymbol? defaultTargetType,
            XamlCompileContext ctx)
        {
            if (condEl.Name.LocalName != "Condition") return false;
            string? propAttr = condEl.Attribute("Property")?.Value;
            string? valAttr = condEl.Attribute("Value")?.Value;
            string? sourceName = condEl.Attribute("SourceName")?.Value;
            if (string.IsNullOrWhiteSpace(propAttr) || valAttr is null) return false;

            INamedTypeSymbol? effectiveOwner = (!string.IsNullOrEmpty(sourceName) && ctx.TemplateNamedElementTypes.TryGetValue(sourceName!, out var snType))
                ? snType
                : defaultTargetType;
            if (!TryResolveDependencyPropertyRef(propAttr!, effectiveOwner, condEl, ctx, out string dpExpr, out ITypeSymbol? propClrType))
            {
                return false;
            }

            ITypeSymbol valueType = propClrType ?? ctx.Compilation.GetSpecialType(SpecialType.System_Object);
            if (!TryResolveAttributeValueExpr(valAttr, valueType, condEl, ctx, out string valExpr))
            {
                return false;
            }

            sb.Append(condsExpr).Append(".Add(new global::System.Windows.Condition(").Append(dpExpr).Append(", ").Append(valExpr);
            if (!string.IsNullOrEmpty(sourceName))
            {
                sb.Append(", ").Append(ToCSharpStringLiteral(sourceName!));
            }
            sb.AppendLine("));");
            return true;
        }

        private static bool TryEmitBindingCondition(StringBuilder sb, string condsExpr, XElement condEl, INamedTypeSymbol? defaultTargetType, XamlCompileContext ctx)
        {
            if (condEl.Name.LocalName != "Condition") return false;
            if (!TryEmitBindingFromAttrOrChild(sb, condEl, "Binding", "Condition.Binding", ctx, out string bindingExpr))
            {
                return false;
            }
            string? valAttr = condEl.Attribute("Value")?.Value;
            if (valAttr is null) return false;
            ITypeSymbol valueType = TryInferBindingValueType(condEl, "Binding", "Condition.Binding", defaultTargetType, ctx)
                ?? ctx.Compilation.GetSpecialType(SpecialType.System_Object);
            if (!TryResolveAttributeValueExpr(valAttr, valueType, condEl, ctx, out string valExpr))
            {
                return false;
            }
            sb.Append(condsExpr).Append(".Add(new global::System.Windows.Condition(").Append(bindingExpr).Append(", ").Append(valExpr).AppendLine("));");
            return true;
        }

        private static ITypeSymbol? TryInferBindingValueType(
            XElement ownerEl,
            string attrName,
            string childPropName,
            INamedTypeSymbol? defaultTargetType,
            XamlCompileContext ctx)
        {
            string? path = null;
            string? sourceRaw = null;
            string? relSourceRaw = null;
            string? elementName = null;

            string? bAttr = ownerEl.Attribute(attrName)?.Value;
            if (!string.IsNullOrWhiteSpace(bAttr))
            {
                string trimmed = bAttr!.Trim();
                if (!trimmed.StartsWith("{", StringComparison.Ordinal) || !trimmed.EndsWith("}", StringComparison.Ordinal))
                    return null;
                string inner = trimmed.Substring(1, trimmed.Length - 2).Trim();
                if (!inner.Equals("Binding", StringComparison.Ordinal) && !inner.StartsWith("Binding ", StringComparison.Ordinal))
                    return null;

                string args = inner.Length > 7 ? inner.Substring(7).Trim() : string.Empty;
                if (!string.IsNullOrEmpty(args))
                {
                    foreach (string rawPart in SplitMarkupArgs(args))
                    {
                        string part = rawPart.Trim();
                        if (string.IsNullOrEmpty(part)) continue;
                        int eq = part.IndexOf('=');
                        if (eq < 0)
                        {
                            path ??= UnquoteMarkupArg(part);
                            continue;
                        }
                        string k = part.Substring(0, eq).Trim();
                        string rawV = part.Substring(eq + 1).Trim();
                        switch (k)
                        {
                            case "Path": path = UnquoteMarkupArg(rawV); break;
                            case "Source": sourceRaw = rawV; break;
                            case "RelativeSource": relSourceRaw = rawV; break;
                            case "ElementName": elementName = UnquoteMarkupArg(rawV); break;
                        }
                    }
                }
            }
            else
            {
                XElement? childWrap = ownerEl.Elements().FirstOrDefault(e => e.Name.LocalName == childPropName);
                XElement? bEl = childWrap?.Elements().FirstOrDefault();
                if (bEl is null || bEl.Name.LocalName != "Binding")
                    return null;
                path = bEl.Attribute("Path")?.Value;
                sourceRaw = bEl.Attribute("Source")?.Value;
                relSourceRaw = bEl.Attribute("RelativeSource")?.Value;
                elementName = bEl.Attribute("ElementName")?.Value;
            }

            if (string.IsNullOrWhiteSpace(path))
            {
                if (!string.IsNullOrWhiteSpace(sourceRaw))
                {
                    string sTrim = sourceRaw!.Trim();
                    if (sTrim.StartsWith("{x:Static ", StringComparison.Ordinal) && sTrim.EndsWith("}", StringComparison.Ordinal))
                    {
                        string memberRef = sTrim.Substring("{x:Static ".Length, sTrim.Length - "{x:Static ".Length - 1).Trim();
                        int dot = memberRef.LastIndexOf('.');
                        if (dot > 0)
                        {
                            INamedTypeSymbol? staticType = ResolveMarkupType(memberRef.Substring(0, dot).Trim(), ownerEl, ctx);
                            string memberName = memberRef.Substring(dot + 1).Trim();
                            if (staticType is not null)
                            {
                                IPropertySymbol? sp = FindProperty(staticType, memberName);
                                if (sp is not null) return sp.Type;
                                IFieldSymbol? sf = staticType.GetMembers(memberName).OfType<IFieldSymbol>().FirstOrDefault();
                                if (sf is not null) return sf.Type;
                            }
                        }
                    }
                }
                return null;
            }

            ITypeSymbol? currentType = defaultTargetType;
            if (!string.IsNullOrWhiteSpace(elementName) && ctx.TemplateNamedElementTypes.TryGetValue(elementName!, out var elemNamedType))
            {
                currentType = elemNamedType;
            }
            else if (!string.IsNullOrWhiteSpace(relSourceRaw))
            {
                string rsTrim = relSourceRaw!.Trim();
                int ancIdx = rsTrim.IndexOf("AncestorType=", StringComparison.Ordinal);
                if (ancIdx >= 0)
                {
                    string after = rsTrim.Substring(ancIdx + "AncestorType=".Length).Trim();
                    if (after.StartsWith("{x:Type ", StringComparison.Ordinal))
                    {
                        int endBrace = after.IndexOf('}');
                        if (endBrace > 0)
                        {
                            string typePart = after.Substring("{x:Type ".Length, endBrace - "{x:Type ".Length).Trim();
                            INamedTypeSymbol? ancSym = ResolveMarkupType(typePart, ownerEl, ctx);
                            if (ancSym is not null) currentType = ancSym;
                        }
                    }
                }
            }

            // Walk path segments (splitting on '.' outside parentheses)
            int pos = 0;
            string p = path!.Trim();
            while (pos < p.Length)
            {
                int depth = 0;
                int nextDot = -1;
                for (int i = pos; i < p.Length; i++)
                {
                    char ch = p[i];
                    if (ch == '(') depth++;
                    else if (ch == ')') depth--;
                    else if (ch == '.' && depth == 0)
                    {
                        nextDot = i;
                        break;
                    }
                }

                string seg = (nextDot >= 0 ? p.Substring(pos, nextDot - pos) : p.Substring(pos)).Trim();
                pos = nextDot >= 0 ? nextDot + 1 : p.Length;

                if (seg.StartsWith("(", StringComparison.Ordinal) && seg.EndsWith(")", StringComparison.Ordinal))
                {
                    string inner = seg.Substring(1, seg.Length - 2).Trim();
                    int dot = inner.LastIndexOf('.');
                    if (dot <= 0) return null;
                    INamedTypeSymbol? propOwner = ResolveMarkupType(inner.Substring(0, dot).Trim(), ownerEl, ctx);
                    string propName = inner.Substring(dot + 1).Trim();
                    if (propOwner is null) return null;
                    currentType = FindProperty(propOwner, propName)?.Type
                        ?? FindStaticGetMethod(propOwner, "Get" + propName)?.ReturnType;
                }
                else
                {
                    if (currentType is not INamedTypeSymbol named) return null;
                    currentType = FindProperty(named, seg)?.Type;
                }

                if (currentType is null) return null;
            }

            return currentType;
        }

        private static bool TryEmitBindingFromAttrOrChild(
            StringBuilder sb,
            XElement ownerEl,
            string attrName,
            string childPropName,
            XamlCompileContext ctx,
            out string bindingExpr)
        {
            bindingExpr = string.Empty;
            string? bAttr = ownerEl.Attribute(attrName)?.Value;
            if (!string.IsNullOrWhiteSpace(bAttr))
            {
                string trimmed = bAttr!.Trim();
                if (trimmed.StartsWith("{", StringComparison.Ordinal) && trimmed.EndsWith("}", StringComparison.Ordinal))
                {
                    string inner = trimmed.Substring(1, trimmed.Length - 2).Trim();
                    if (inner.Equals("Binding", StringComparison.Ordinal) || inner.StartsWith("Binding ", StringComparison.Ordinal))
                    {
                        return TryBuildBindingExpression(inner, ownerEl, ctx, out bindingExpr);
                    }
                }
                return false;
            }
            XElement? childWrap = ownerEl.Elements().FirstOrDefault(e => e.Name.LocalName == childPropName);
            XElement? bEl = childWrap?.Elements().FirstOrDefault();
            if (bEl is null) return false;
            var dummyBind = new StringBuilder();
            return TryEmitAnyValueElement(sb, dummyBind, bEl, ctx, out bindingExpr) && dummyBind.Length == 0;
        }

        private static bool TryEmitSetterToCollection(
            StringBuilder sb,
            string settersExpr,
            XElement setterEl,
            INamedTypeSymbol? defaultTargetType,
            bool allowTargetName,
            XamlCompileContext ctx)
        {
            string? propAttr = setterEl.Attribute("Property")?.Value;
            if (string.IsNullOrWhiteSpace(propAttr))
            {
                return false;
            }

            string? targetName = allowTargetName ? setterEl.Attribute("TargetName")?.Value : null;
            INamedTypeSymbol? effectiveOwner = (!string.IsNullOrEmpty(targetName) && ctx.TemplateNamedElementTypes.TryGetValue(targetName!, out var tnType))
                ? tnType
                : defaultTargetType;

            if (!TryResolveDependencyPropertyRef(propAttr!, effectiveOwner, setterEl, ctx, out string dpExpr, out ITypeSymbol? propClrType))
            {
                return false;
            }

            XAttribute? valAttr = setterEl.Attribute("Value");
            string? valExpr = null;

            if (valAttr is not null)
            {
                string trimmed = valAttr.Value.Trim();
                if (trimmed.StartsWith("{DynamicResource ", StringComparison.Ordinal) && trimmed.EndsWith("}", StringComparison.Ordinal))
                {
                    string keyRaw = trimmed.Substring("{DynamicResource ".Length, trimmed.Length - "{DynamicResource ".Length - 1).Trim();
                    if (!TryResolveMarkupOrLiteralKeyExpr(keyRaw, setterEl, ctx, out string keyExpr))
                    {
                        return false;
                    }
                    valExpr = $"new global::System.Windows.DynamicResourceExtension({keyExpr})";
                }
                else
                {
                    ITypeSymbol targetValType = propClrType ?? ctx.Compilation.GetSpecialType(SpecialType.System_Object);
                    if (!TryResolveAttributeValueExpr(valAttr.Value, targetValType, setterEl, ctx, out valExpr))
                    {
                        return false;
                    }
                }
            }
            else
            {
                XElement? setterValueEl = setterEl.Elements().FirstOrDefault(static e => e.Name.LocalName == "Setter.Value");
                XElement? contentEl = setterValueEl?.Elements().FirstOrDefault() ?? setterEl.Elements().FirstOrDefault();
                if (contentEl is null)
                {
                    return false;
                }

                if (contentEl.Name.LocalName == "ControlTemplate")
                {
                    if (!TryEmitControlTemplateElement(sb, contentEl, defaultTargetType, ctx, out valExpr))
                    {
                        return false;
                    }
                }
                else if (contentEl.Name.LocalName == "ItemsPanelTemplate")
                {
                    if (!TryEmitItemsPanelTemplateElement(sb, contentEl, ctx, out valExpr))
                    {
                        return false;
                    }
                }
                else
                {
                    var dummyBindingSb = new StringBuilder();
                    if (!TryEmitAnyValueElement(sb, dummyBindingSb, contentEl, ctx, out valExpr) || dummyBindingSb.Length > 0)
                    {
                        return false;
                    }
                }
            }

            sb.Append(settersExpr).Append(".Add(new global::System.Windows.Setter(").Append(dpExpr).Append(", ").Append(valExpr);
            if (!string.IsNullOrEmpty(targetName))
            {
                sb.Append(", ").Append(ToCSharpStringLiteral(targetName!));
            }
            sb.AppendLine("));");
            return true;
        }

        private static bool TryResolveDependencyPropertyRef(
            string propNameExpr,
            INamedTypeSymbol? defaultTargetType,
            XElement contextEl,
            XamlCompileContext ctx,
            out string dpExpr,
            out ITypeSymbol? clrType)
        {
            dpExpr = string.Empty;
            clrType = null;

            string trimmed = propNameExpr.Trim();
            if (trimmed.StartsWith("(", StringComparison.Ordinal) && trimmed.EndsWith(")", StringComparison.Ordinal))
            {
                trimmed = trimmed.Substring(1, trimmed.Length - 2).Trim();
            }
            int dot = trimmed.IndexOf('.');
            INamedTypeSymbol? ownerType = defaultTargetType;
            string dpShortName = trimmed;

            if (dot > 0)
            {
                string ownerPart = trimmed.Substring(0, dot);
                dpShortName = trimmed.Substring(dot + 1);
                ownerType = ResolveMarkupType(ownerPart, contextEl, ctx);
            }

            if (ownerType is null)
            {
                return false;
            }

            ISymbol? dpField = FindDependencyPropertyField(ownerType, dpShortName + "Property");
            if (dpField is null)
            {
                return ctx.Fail($"DP '{ownerType.Name}.{dpShortName}Property' not found");
            }

            dpExpr = dpField.ContainingType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat) + "." + dpField.Name;
            IPropertySymbol? clrProp = FindProperty(ownerType, dpShortName);
            if (clrProp is not null)
            {
                clrType = clrProp.Type;
            }
            else
            {
                IMethodSymbol? getMethod = FindStaticGetMethod(ownerType, "Get" + dpShortName);
                clrType = getMethod?.ReturnType;
            }
            return true;
        }

        private static IMethodSymbol? FindStaticGetMethod(INamedTypeSymbol ownerType, string methodName)
        {
            for (INamedTypeSymbol? cur = ownerType; cur is not null; cur = cur.BaseType)
            {
                foreach (ISymbol member in cur.GetMembers(methodName))
                {
                    if (member is IMethodSymbol m && m.IsStatic && m.DeclaredAccessibility == Accessibility.Public && m.Parameters.Length == 1)
                    {
                        return m;
                    }
                }
            }
            return null;
        }

        private static void CollectTemplateNamedElements(XElement el, string classNamespace, Compilation compilation, string xamlNs, Dictionary<string, INamedTypeSymbol> map)
        {
            string local = el.Name.LocalName;
            if (!local.Contains('.'))
            {
                string? name = el.Attribute(XName.Get("Name", xamlNs))?.Value ?? el.Attribute("Name")?.Value;
                if (!string.IsNullOrEmpty(name))
                {
                    INamedTypeSymbol? elemType = ResolveElementType(local, el.Name.NamespaceName, classNamespace, compilation);
                    if (elemType is not null)
                    {
                        map[name!] = elemType;
                    }
                }
            }
            foreach (XElement child in el.Elements())
            {
                if (child.Name.LocalName == "ControlTemplate" || child.Name.LocalName == "DataTemplate")
                {
                    continue; // Inner templates have their own namescope
                }
                CollectTemplateNamedElements(child, classNamespace, compilation, xamlNs, map);
            }
        }

        private static bool TryEmitControlTemplateElement(
            StringBuilder sb,
            XElement tplEl,
            INamedTypeSymbol? inheritedTargetType,
            XamlCompileContext ctx,
            out string tplVar)
        {
            tplVar = ctx.NextVar("__ctpl");
            INamedTypeSymbol? targetType = inheritedTargetType;
            string? targetAttr = tplEl.Attribute("TargetType")?.Value;
            if (!string.IsNullOrWhiteSpace(targetAttr))
            {
                if (!TryResolveTypeAttributeExpr(targetAttr!, tplEl, ctx, out targetType, out string typeofExpr))
                {
                    return false;
                }
                sb.Append("var ").Append(tplVar).Append(" = new global::System.Windows.Controls.ControlTemplate(").Append(typeofExpr).AppendLine(");");
            }
            else if (targetType is not null)
            {
                sb.Append("var ").Append(tplVar).Append(" = new global::System.Windows.Controls.ControlTemplate(typeof(")
                  .Append(targetType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)).AppendLine("));");
            }
            else
            {
                sb.Append("var ").Append(tplVar).AppendLine(" = new global::System.Windows.Controls.ControlTemplate();");
            }

            XElement? resourcesEl = null;
            XElement? triggersEl = null;
            XElement? visualRoot = null;
            foreach (XElement child in tplEl.Elements())
            {
                string local = child.Name.LocalName;
                if (local == "ControlTemplate.Resources")
                {
                    resourcesEl = child;
                }
                else if (local == "ControlTemplate.Triggers")
                {
                    triggersEl = child;
                }
                else if (local == "ControlTemplate.VisualTree")
                {
                    visualRoot = child.Elements().FirstOrDefault();
                }
                else if (visualRoot is null && !local.Contains('.'))
                {
                    visualRoot = child;
                }
                else
                {
                    return false;
                }
            }

            var savedNamed = new Dictionary<string, INamedTypeSymbol>(ctx.TemplateNamedElementTypes, StringComparer.Ordinal);
            ctx.TemplateNamedElementTypes.Clear();
            if (visualRoot is not null)
            {
                CollectTemplateNamedElements(visualRoot, ctx.ClassNamespace, ctx.Compilation, ctx.XamlNs, ctx.TemplateNamedElementTypes);
            }

            bool pushedTplRes = false;
            try
            {
                if (resourcesEl is not null)
                {
                    var dummy = new StringBuilder();
                    if (!TryEmitResourcesCollection(sb, dummy, tplVar + ".Resources", resourcesEl, ctx) || dummy.Length > 0)
                    {
                        return false;
                    }
                    var tplResKeys = new HashSet<string>(StringComparer.Ordinal);
                    foreach (XElement kEl in resourcesEl.Elements())
                    {
                        string? rawK = kEl.Attribute(XName.Get("Key", ctx.XamlNs))?.Value;
                        if (!string.IsNullOrWhiteSpace(rawK))
                        {
                            tplResKeys.Add(rawK!);
                            tplResKeys.Add(NormalizeThemeResourceKey(rawK!));
                        }
                    }
                    ctx.LocalResourceKeyScopes.Push(tplResKeys);
                    ctx.ResourceScopes.Insert(0, (tplVar + ".Resources", true));
                    pushedTplRes = true;
                }

                if (visualRoot is not null)
                {
                    if (!TryEmitFrameworkElementFactoryTree(sb, visualRoot, targetType, ctx, out string rootFefVar))
                    {
                        return false;
                    }
                    sb.Append(tplVar).Append(".VisualTree = ").Append(rootFefVar).AppendLine(";");
                }

                if (triggersEl is not null)
                {
                    foreach (XElement trigEl in triggersEl.Elements())
                    {
                        if (!TryEmitTriggerToCollection(sb, tplVar + ".Triggers", trigEl, targetType, ctx))
                        {
                            return false;
                        }
                    }
                }

                return true;
            }
            finally
            {
                if (pushedTplRes)
                {
                    ctx.ResourceScopes.RemoveAt(0);
                    ctx.LocalResourceKeyScopes.Pop();
                }
                ctx.TemplateNamedElementTypes.Clear();
                foreach (var kv in savedNamed)
                {
                    ctx.TemplateNamedElementTypes[kv.Key] = kv.Value;
                }
            }
        }

        private static bool TryEmitItemsPanelTemplateElement(
            StringBuilder sb,
            XElement iptEl,
            XamlCompileContext ctx,
            out string iptVar)
        {
            iptVar = ctx.NextVar("__ipt");
            XElement? visualRoot = iptEl.Elements().FirstOrDefault(static e => !e.Name.LocalName.Contains('.'));
            if (visualRoot is null)
            {
                XElement? vt = iptEl.Elements().FirstOrDefault(static e => e.Name.LocalName == "ItemsPanelTemplate.VisualTree");
                visualRoot = vt?.Elements().FirstOrDefault();
            }
            if (visualRoot is not null)
            {
                if (!TryEmitFrameworkElementFactoryTree(sb, visualRoot, templateTargetType: null, ctx, out string rootFefVar))
                {
                    return false;
                }
                sb.Append("var ").Append(iptVar).Append(" = new global::System.Windows.Controls.ItemsPanelTemplate(").Append(rootFefVar).AppendLine(");");
            }
            else
            {
                sb.Append("var ").Append(iptVar).AppendLine(" = new global::System.Windows.Controls.ItemsPanelTemplate();");
            }
            return true;
        }

        private static bool TryEmitDataTemplateElement(
            StringBuilder sb,
            XElement dtEl,
            XamlCompileContext ctx,
            out string dtVar)
        {
            dtVar = ctx.NextVar("__dtpl");
            string? dataTypeAttr = dtEl.Attribute("DataType")?.Value;
            if (!string.IsNullOrWhiteSpace(dataTypeAttr))
            {
                if (!TryResolveTypeAttributeExpr(dataTypeAttr!, dtEl, ctx, out _, out string typeofExpr))
                {
                    return false;
                }
                sb.Append("var ").Append(dtVar).Append(" = new global::System.Windows.DataTemplate(").Append(typeofExpr).AppendLine(");");
            }
            else
            {
                sb.Append("var ").Append(dtVar).AppendLine(" = new global::System.Windows.DataTemplate();");
            }

            XElement? visualRoot = null;
            foreach (XElement child in dtEl.Elements())
            {
                string local = child.Name.LocalName;
                if (local == "DataTemplate.Resources")
                {
                    var dummy = new StringBuilder();
                    if (!TryEmitResourcesCollection(sb, dummy, dtVar + ".Resources", child, ctx) || dummy.Length > 0)
                    {
                        return false;
                    }
                }
                else if (local == "DataTemplate.VisualTree")
                {
                    visualRoot = child.Elements().FirstOrDefault();
                }
                else if (visualRoot is null && !local.Contains('.'))
                {
                    visualRoot = child;
                }
                else
                {
                    return false;
                }
            }

            if (visualRoot is not null)
            {
                if (!TryEmitFrameworkElementFactoryTree(sb, visualRoot, templateTargetType: null, ctx, out string rootFefVar))
                {
                    return false;
                }
                sb.Append(dtVar).Append(".VisualTree = ").Append(rootFefVar).AppendLine(";");
            }

            return true;
        }

        private static bool TryEmitFrameworkElementFactoryTree(
            StringBuilder sb,
            XElement el,
            INamedTypeSymbol? templateTargetType,
            XamlCompileContext ctx,
            out string fefVar)
        {
            fefVar = string.Empty;
            INamedTypeSymbol? elemType = ResolveElementType(el.Name.LocalName, el.Name.NamespaceName, ctx.ClassNamespace, ctx.Compilation);
            if (elemType is null || elemType.IsAbstract || !HasPublicParameterlessCtor(elemType))
            {
                return ctx.Fail($"FEF element '{el.Name.LocalName}' not instantiable");
            }

            string? name = el.Attribute(XName.Get("Name", ctx.XamlNs))?.Value ?? el.Attribute("Name")?.Value;
            fefVar = ctx.NextVar("__fef");
            string fullElemType = elemType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
            sb.Append("var ").Append(fefVar).Append(" = global::System.Windows.FrameworkElementFactory.Create<").Append(fullElemType).Append(">(");
            if (!string.IsNullOrEmpty(name))
            {
                sb.Append(ToCSharpStringLiteral(name!));
            }
            sb.AppendLine(");");
            if (!string.IsNullOrEmpty(name))
            {
                ISymbol? nameDp = FindDependencyPropertyField(elemType, "NameProperty");
                if (nameDp != null)
                {
                    sb.Append(fefVar).Append(".SetValue(")
                      .Append(nameDp.ContainingType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat))
                      .Append('.').Append(nameDp.Name)
                      .Append(", ").Append(ToCSharpStringLiteral(name!)).AppendLine(");");
                }
            }

            int pushedFefResScopes = 0;
            foreach (XElement resEl in el.Elements())
            {
                string rl = resEl.Name.LocalName;
                int dotIdx = rl.IndexOf('.');
                if (dotIdx > 0 && rl.Substring(dotIdx + 1) == "Resources")
                {
                    string fefResVar = ctx.NextVar("__fefRes");
                    sb.Append("var ").Append(fefResVar).AppendLine(" = new global::System.Windows.ResourceDictionary();");
                    var dummyBind = new StringBuilder();
                    if (!TryEmitResourcesCollection(sb, dummyBind, fefResVar, resEl, ctx) || dummyBind.Length > 0)
                    {
                        while (pushedFefResScopes-- > 0)
                        {
                            ctx.ResourceScopes.RemoveAt(0);
                            ctx.LocalResourceKeyScopes.Pop();
                        }
                        return false;
                    }
                    var localKeys = new HashSet<string>(StringComparer.Ordinal);
                    foreach (XElement kEl in resEl.Elements())
                    {
                        string? rawK = kEl.Attribute(XName.Get("Key", ctx.XamlNs))?.Value;
                        if (!string.IsNullOrWhiteSpace(rawK))
                        {
                            localKeys.Add(rawK!);
                            localKeys.Add(NormalizeThemeResourceKey(rawK!));
                        }
                    }
                    ctx.LocalResourceKeyScopes.Push(localKeys);
                    ctx.ResourceScopes.Insert(0, (fefResVar, true));
                    pushedFefResScopes++;
                    sb.Append(fefVar).Append(".AddPostInstantiateAction(__feObj => ((global::System.Windows.FrameworkElement)__feObj).Resources.MergedDictionaries.Add(").Append(fefResVar).AppendLine("));");
                }
            }

            try
            {
            foreach (XAttribute attr in el.Attributes())
            {
                if (attr.IsNamespaceDeclaration) continue;
                string attrNs = attr.Name.NamespaceName;
                string attrLocal = attr.Name.LocalName;
                if (attrNs == "http://www.w3.org/XML/1998/namespace") continue;
                if (attrNs == ctx.XamlNs)
                {
                    if (attrLocal == "Name") continue;
                    return false;
                }
                if (attrLocal == "Name") continue;
                if (attrNs == "http://schemas.openxmlformats.org/markup-compatibility/2006" ||
                    attrNs == "http://schemas.microsoft.com/expression/blend/2008" ||
                    attrNs == "http://www.w3.org/XML/1998/namespace")
                {
                    continue;
                }

                string dpExpr;
                ITypeSymbol? valType;
                int dotIdx = attrLocal.IndexOf('.');
                if (dotIdx > 0 || !string.IsNullOrEmpty(attrNs))
                {
                    if (dotIdx <= 0) return false;
                    string ownerName = attrLocal.Substring(0, dotIdx);
                    string propName = attrLocal.Substring(dotIdx + 1);
                    INamedTypeSymbol? ownerType = ResolveElementType(ownerName, string.IsNullOrEmpty(attrNs) ? el.Name.NamespaceName : attrNs, ctx.ClassNamespace, ctx.Compilation);
                    if (ownerType is null) return false;
                    ISymbol? attachedDp = FindDependencyPropertyField(ownerType, propName + "Property");
                    if (attachedDp is null) return ctx.Fail($"Attached DP '{ownerType.Name}.{propName}Property' not found");
                    dpExpr = attachedDp.ContainingType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat) + "." + attachedDp.Name;
                    valType = FindStaticGetMethod(ownerType, "Get" + propName)?.ReturnType;
                }
                else
                {
                    ISymbol? dpField = FindDependencyPropertyField(elemType, attrLocal + "Property");
                    if (dpField is null) return ctx.Fail($"FEF attr DP '{elemType.Name}.{attrLocal}Property' not found");
                    dpExpr = dpField.ContainingType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat) + "." + dpField.Name;
                    valType = FindProperty(elemType, attrLocal)?.Type;
                }

                string trimmed = attr.Value.Trim();
                if (trimmed.StartsWith("{TemplateBinding ", StringComparison.Ordinal) && trimmed.EndsWith("}", StringComparison.Ordinal))
                {
                    string tbArgs = trimmed.Substring("{TemplateBinding ".Length, trimmed.Length - "{TemplateBinding ".Length - 1).Trim();
                    string? srcPropName = null;
                    string? converterExpr = null;
                    string? converterParamExpr = null;
                    INamedTypeSymbol? converterType = ctx.Compilation.GetTypeByMetadataName("System.Windows.Data.IValueConverter");
                    ITypeSymbol objType = ctx.Compilation.GetSpecialType(SpecialType.System_Object);

                    foreach (string rawPart in SplitMarkupArgs(tbArgs))
                    {
                        string part = rawPart.Trim();
                        if (string.IsNullOrEmpty(part)) continue;
                        int eq = part.IndexOf('=');
                        if (eq < 0)
                        {
                            srcPropName ??= UnquoteMarkupArg(part);
                            continue;
                        }
                        string k = part.Substring(0, eq).Trim();
                        string rawV = part.Substring(eq + 1).Trim();
                        switch (k)
                        {
                            case "Property":
                                srcPropName = UnquoteMarkupArg(rawV);
                                break;
                            case "Converter":
                                if (!TryResolveMarkupValueExpr(rawV, el, converterType, ctx, out converterExpr))
                                    return false;
                                break;
                            case "ConverterParameter":
                                if (!TryResolveAttributeValueExpr(rawV, objType, el, ctx, out converterParamExpr))
                                    return false;
                                break;
                            default:
                                return false;
                        }
                    }

                    if (string.IsNullOrWhiteSpace(srcPropName))
                    {
                        return false;
                    }

                    INamedTypeSymbol? srcOwner = templateTargetType ?? elemType;
                    if (!TryResolveDependencyPropertyRef(srcPropName!, srcOwner, el, ctx, out string srcDpExpr, out _))
                    {
                        return false;
                    }

                    sb.Append(fefVar).Append(".SetValue(").Append(dpExpr).Append(", new global::System.Windows.TemplateBindingExtension(").Append(srcDpExpr).Append(')');
                    var tbInits = new List<string>();
                    if (converterExpr is not null) tbInits.Add("Converter = " + converterExpr);
                    if (converterParamExpr is not null) tbInits.Add("ConverterParameter = " + converterParamExpr);
                    if (tbInits.Count > 0)
                    {
                        sb.Append(" { ").Append(string.Join(", ", tbInits)).Append(" }");
                    }
                    sb.AppendLine(");");
                    continue;
                }

                if (trimmed.StartsWith("{Binding", StringComparison.Ordinal) && trimmed.EndsWith("}", StringComparison.Ordinal))
                {
                    if (!TryBuildBindingExpression(trimmed.Substring(1, trimmed.Length - 2).Trim(), el, ctx, out string bindingExpr))
                    {
                        return false;
                    }
                    sb.Append(fefVar).Append(".SetBinding(").Append(dpExpr).Append(", ").Append(bindingExpr).AppendLine(");");
                    continue;
                }

                if (trimmed.StartsWith("{DynamicResource ", StringComparison.Ordinal) && trimmed.EndsWith("}", StringComparison.Ordinal))
                {
                    string keyRaw = trimmed.Substring("{DynamicResource ".Length, trimmed.Length - "{DynamicResource ".Length - 1).Trim();
                    if (!TryResolveMarkupOrLiteralKeyExpr(keyRaw, el, ctx, out string keyExpr))
                    {
                        return false;
                    }
                    sb.Append(fefVar).Append(".SetResourceReference(").Append(dpExpr).Append(", ").Append(keyExpr).AppendLine(");");
                    continue;
                }

                if (!TryResolveAttributeValueExpr(attr.Value, valType ?? ctx.Compilation.GetSpecialType(SpecialType.System_Object), el, ctx, out string valExpr))
                {
                    return ctx.Fail($"FEF attr '{attrLocal}' value '{attr.Value}' on '{elemType.Name}' failed");
                }
                sb.Append(fefVar).Append(".SetValue(").Append(dpExpr).Append(", ").Append(valExpr).AppendLine(");");
            }

            foreach (XElement child in el.Elements())
            {
                string childLocal = child.Name.LocalName;
                if (childLocal.Contains('.'))
                {
                    int dotIdx = childLocal.IndexOf('.');
                    string ownerName = childLocal.Substring(0, dotIdx);
                    string propName = childLocal.Substring(dotIdx + 1);

                    if (propName == "ColumnDefinitions")
                    {
                        foreach (XElement colEl in child.Elements())
                        {
                            if (!TryEmitFrameworkElementFactoryTree(sb, colEl, templateTargetType, ctx, out string colFefVar))
                            {
                                return false;
                            }
                            sb.Append(fefVar).Append(".AppendChildProperty(static (p, c) => ((global::System.Windows.Controls.Grid)p).ColumnDefinitions.Add((global::System.Windows.Controls.ColumnDefinition)c), ").Append(colFefVar).AppendLine(");");
                        }
                        continue;
                    }

                    if (propName == "RowDefinitions")
                    {
                        foreach (XElement rowEl in child.Elements())
                        {
                            if (!TryEmitFrameworkElementFactoryTree(sb, rowEl, templateTargetType, ctx, out string rowFefVar))
                            {
                                return false;
                            }
                            sb.Append(fefVar).Append(".AppendChildProperty(static (p, c) => ((global::System.Windows.Controls.Grid)p).RowDefinitions.Add((global::System.Windows.Controls.RowDefinition)c), ").Append(rowFefVar).AppendLine(");");
                        }
                        continue;
                    }

                    if (propName == "Resources")
                    {
                        continue; // Already emitted and pushed before attributes/children
                    }

                    if (childLocal == "VisualStateManager.VisualStateGroups")
                    {
                        var vsgSb = new StringBuilder();
                        var dummyBind = new StringBuilder();
                        vsgSb.AppendLine("var __vsgList = global::System.Windows.VisualStateManager.GetVisualStateGroups((global::System.Windows.FrameworkElement)__feObj);");
                        foreach (XElement vsgEl in child.Elements())
                        {
                            if (!TryEmitAnyValueElement(vsgSb, dummyBind, vsgEl, ctx, out string vsgVar) || dummyBind.Length > 0)
                            {
                                return ctx.Fail($"VisualStateGroups child '{vsgEl.Name.LocalName}' failed");
                            }
                            vsgSb.Append("__vsgList.Add(").Append(vsgVar).AppendLine(");");
                        }
                        sb.Append(fefVar).AppendLine(".AddPostInstantiateAction(__feObj =>");
                        sb.AppendLine("{");
                        using (var r = new StringReader(vsgSb.ToString()))
                        {
                            string? l;
                            while ((l = r.ReadLine()) is not null)
                            {
                                sb.Append("    ").AppendLine(l);
                            }
                        }
                        sb.AppendLine("});");
                        continue;
                    }

                    // Check if property is a DependencyProperty on elemType or attached owner
                    INamedTypeSymbol? propOwner = elemType;
                    ISymbol? dpField = FindDependencyPropertyField(elemType, propName + "Property");
                    if (dpField is null)
                    {
                        INamedTypeSymbol? explicitOwner = ResolveElementType(ownerName, child.Name.NamespaceName, ctx.ClassNamespace, ctx.Compilation);
                        if (explicitOwner is not null)
                        {
                            dpField = FindDependencyPropertyField(explicitOwner, propName + "Property");
                            propOwner = explicitOwner;
                        }
                    }

                    if (dpField is not null && !GetMemberType(dpField).ToDisplayString().EndsWith("DependencyPropertyKey", StringComparison.Ordinal))
                    {
                        string dpExpr = dpField.ContainingType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat) + "." + dpField.Name;
                        XElement? valEl = child.Elements().FirstOrDefault();
                        if (valEl is null) return false;
                        if (valEl.Name.LocalName == "ControlTemplate")
                        {
                            if (!TryEmitControlTemplateElement(sb, valEl, elemType, ctx, out string ctVar))
                            {
                                return false;
                            }
                            sb.Append(fefVar).Append(".SetValue(").Append(dpExpr).Append(", ").Append(ctVar).AppendLine(");");
                            continue;
                        }
                        var dummyBind = new StringBuilder();
                        if (!TryEmitAnyValueElement(sb, dummyBind, valEl, ctx, out string valVar) || dummyBind.Length > 0)
                        {
                            return ctx.Fail($"FEF prop element '{childLocal}' val '{valEl.Name.LocalName}' failed");
                        }
                        INamedTypeSymbol? valElemType = ResolveElementType(valEl.Name.LocalName, valEl.Name.NamespaceName, ctx.ClassNamespace, ctx.Compilation);
                        if (valElemType is not null && InheritsFrom(valElemType, "System.Windows.Data.BindingBase"))
                        {
                            sb.Append(fefVar).Append(".SetBinding(").Append(dpExpr).Append(", ").Append(valVar).AppendLine(");");
                        }
                        else
                        {
                            sb.Append(fefVar).Append(".SetValue(").Append(dpExpr).Append(", ").Append(valVar).AppendLine(");");
                        }
                        continue;
                    }

                    // CLR child element property (e.g. Track.DecreaseRepeatButton, Track.Thumb, BulletDecorator.Bullet)
                    IPropertySymbol? clrProp = FindProperty(elemType, propName);
                    if (clrProp is not null && clrProp.SetMethod is not null && clrProp.SetMethod.DeclaredAccessibility == Accessibility.Public)
                    {
                        XElement? innerChild = child.Elements().FirstOrDefault();
                        if (innerChild is not null)
                        {
                            if (!TryEmitFrameworkElementFactoryTree(sb, innerChild, templateTargetType, ctx, out string innerFefVar))
                            {
                                return false;
                            }
                            string pType = elemType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
                            string cType = clrProp.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
                            sb.Append(fefVar).Append(".AppendChildProperty(static (p, c) => ((").Append(pType).Append(")p).").Append(clrProp.Name)
                              .Append(" = (").Append(cType).Append(")c, ").Append(innerFefVar).AppendLine(");");
                            continue;
                        }
                    }

                    // Fallback: generic wrapper (e.g. Border.Child)
                    foreach (XElement innerChild in child.Elements())
                    {
                        if (!TryEmitFrameworkElementFactoryTree(sb, innerChild, templateTargetType, ctx, out string innerFefVar))
                        {
                            return false;
                        }
                        sb.Append(fefVar).Append(".AppendChild(").Append(innerFefVar).AppendLine(");");
                    }
                    continue;
                }

                if (!TryEmitFrameworkElementFactoryTree(sb, child, templateTargetType, ctx, out string childFefVar))
                {
                    return false;
                }
                sb.Append(fefVar).Append(".AppendChild(").Append(childFefVar).AppendLine(");");
            }

            return true;
            }
            finally
            {
                while (pushedFefResScopes-- > 0)
                {
                    ctx.ResourceScopes.RemoveAt(0);
                    ctx.LocalResourceKeyScopes.Pop();
                }
            }
        }

        private static bool TryCreateAndEmitChildElement(
            StringBuilder sb,
            StringBuilder bindingSb,
            XElement childEl,
            XamlCompileContext ctx,
            out string varName,
            out INamedTypeSymbol childType)
        {
            varName = string.Empty;
            INamedTypeSymbol? resolved = ResolveElementType(childEl.Name.LocalName, childEl.Name.NamespaceName, ctx.ClassNamespace, ctx.Compilation);
            if (resolved is null || resolved.IsAbstract || !HasPublicParameterlessCtor(resolved))
            {
                childType = null!;
                return ctx.Fail($"Child element '{childEl.Name.LocalName}' not instantiable (resolved={resolved?.Name})");
            }

            childType = resolved;
            varName = ctx.NextVar();
            string fullType = resolved.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
            sb.Append("var ").Append(varName).Append(" = new ").Append(fullType).AppendLine("();");

            return TryEmitElementPopulation(sb, bindingSb, childEl, varName, resolved, isRoot: false, ctx);
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
            string text,
            XElement el,
            XamlCompileContext ctx)
        {
            string? contentPropName = FindContentPropertyName(targetType);
            if (!string.IsNullOrEmpty(contentPropName))
            {
                IPropertySymbol? prop = FindProperty(targetType, contentPropName!);
                if (prop is not null && prop.SetMethod is not null && prop.SetMethod.DeclaredAccessibility == Accessibility.Public &&
                    TryConvertLiteralToCSharp(text, prop.Type, el, ctx.ClassNamespace, ctx.Compilation, out string expr))
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
            StringBuilder bindingSb,
            XElement el,
            string targetExpr,
            INamedTypeSymbol ownerType,
            string propName,
            string rawValue,
            XamlCompileContext ctx)
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
                return ctx.Fail($"Attached setter '{ownerType.Name}.{setterName}' not found");
            }

            ITypeSymbol paramType = setMethod.Parameters[1].Type;
            string trimmed = rawValue.Trim();
            if (trimmed == "{x:Null}")
            {
                if (paramType.IsValueType && paramType.OriginalDefinition.SpecialType != SpecialType.System_Nullable_T)
                {
                    return false;
                }
                sb.Append(ownerType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat))
                  .Append('.').Append(setterName).Append('(').Append(targetExpr).AppendLine(", null);");
                return true;
            }

            if (trimmed.StartsWith("{", StringComparison.Ordinal) && trimmed.EndsWith("}", StringComparison.Ordinal))
            {
                string inner = trimmed.Substring(1, trimmed.Length - 2).Trim();
                if (inner.Equals("Binding", StringComparison.Ordinal) || inner.StartsWith("Binding ", StringComparison.Ordinal))
                {
                    ISymbol? dpField = FindDependencyPropertyField(ownerType, propName + "Property");
                    if (dpField is null) return ctx.Fail($"Attached Binding DP '{ownerType.Name}.{propName}Property' not found");
                    if (!TryBuildBindingExpression(inner, el, ctx, out string bindingExpr)) return false;
                    bindingSb.Append("global::System.Windows.Data.BindingOperations.SetBinding(")
                             .Append(targetExpr).Append(", ")
                             .Append(dpField.ContainingType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat))
                             .Append('.').Append(dpField.Name).Append(", ").Append(bindingExpr).AppendLine(");");
                    return true;
                }

                if (inner.StartsWith("DynamicResource ", StringComparison.Ordinal))
                {
                    ISymbol? dpField = FindDependencyPropertyField(ownerType, propName + "Property");
                    if (dpField is not null)
                    {
                        string keyRaw = inner.Substring("DynamicResource ".Length).Trim();
                        if (!TryResolveMarkupOrLiteralKeyExpr(keyRaw, el, ctx, out string keyExpr)) return false;
                        sb.Append(targetExpr).Append(".SetResourceReference(")
                          .Append(dpField.ContainingType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat))
                          .Append('.').Append(dpField.Name).Append(", ").Append(keyExpr).AppendLine(");");
                        return true;
                    }
                }

                if (!TryResolveMarkupValueExpr(trimmed, el, paramType, ctx, out string markupExpr))
                {
                    return false;
                }

                sb.Append(ownerType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat))
                  .Append('.').Append(setterName).Append('(').Append(targetExpr).Append(", ").Append(markupExpr).AppendLine(");");
                return true;
            }

            if (!TryConvertLiteralToCSharp(rawValue, paramType, el, ctx.ClassNamespace, ctx.Compilation, out string valExpr))
            {
                return ctx.Fail($"Attached literal '{ownerType.Name}.{propName}'='{rawValue}' ({paramType.ToDisplayString()}) failed");
            }

            sb.Append(ownerType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat))
              .Append('.').Append(setterName).Append('(').Append(targetExpr).Append(", ").Append(valExpr).AppendLine(");");
            return true;
        }

        private static bool TryResolveAttributeValueExpr(
            string rawValue,
            ITypeSymbol targetType,
            XElement el,
            XamlCompileContext ctx,
            out string expr)
        {
            string trimmed = rawValue.Trim();
            if (trimmed.StartsWith("{", StringComparison.Ordinal) && trimmed.EndsWith("}", StringComparison.Ordinal))
            {
                return TryResolveMarkupValueExpr(trimmed, el, targetType, ctx, out expr);
            }
            return TryConvertLiteralToCSharp(rawValue, targetType, el, ctx.ClassNamespace, ctx.Compilation, out expr);
        }

        private static bool TryResolveMarkupValueExpr(
            string expr,
            XElement el,
            ITypeSymbol? targetType,
            XamlCompileContext ctx,
            out string resolvedExpr)
        {
            resolvedExpr = string.Empty;
            if (string.Equals(expr, "{x:Null}", StringComparison.Ordinal))
            {
                if (targetType is not null && targetType.IsValueType && targetType.OriginalDefinition.SpecialType != SpecialType.System_Nullable_T)
                {
                    return false;
                }
                resolvedExpr = "null!";
                return true;
            }

            if (expr.Length < 2 || expr[0] != '{' || expr[expr.Length - 1] != '}')
            {
                return false;
            }

            string inner = expr.Substring(1, expr.Length - 2).Trim();

            if (inner.StartsWith("x:Static ", StringComparison.Ordinal))
            {
                string memberRef = inner.Substring("x:Static ".Length).Trim();
                int dot = memberRef.LastIndexOf('.');
                if (dot <= 0) return false;
                string typePart = memberRef.Substring(0, dot).Trim();
                string memberName = memberRef.Substring(dot + 1).Trim();
                INamedTypeSymbol? staticType = ResolveMarkupType(typePart, el, ctx);
                if (staticType is null) return false;
                resolvedExpr = staticType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat) + "." + memberName;
                return true;
            }

            if (inner.StartsWith("x:Type ", StringComparison.Ordinal))
            {
                string typePart = inner.Substring("x:Type ".Length).Trim();
                INamedTypeSymbol? sym = ResolveMarkupType(typePart, el, ctx);
                if (sym is null) return false;
                resolvedExpr = "typeof(" + sym.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat) + ")";
                return true;
            }

            if (inner.StartsWith("x:Reference ", StringComparison.Ordinal) || inner.StartsWith("Reference ", StringComparison.Ordinal))
            {
                string refName = inner.StartsWith("x:Reference ", StringComparison.Ordinal)
                    ? inner.Substring("x:Reference ".Length).Trim()
                    : inner.Substring("Reference ".Length).Trim();
                if (refName.StartsWith("Name=", StringComparison.Ordinal))
                {
                    refName = refName.Substring("Name=".Length).Trim();
                }
                refName = refName.Trim('\'', '"');
                if (string.IsNullOrWhiteSpace(refName)) return false;
                string lookup = $"((global::System.Windows.FrameworkElement)this).FindName({ToCSharpStringLiteral(refName)})";
                if (targetType is not null && targetType.SpecialType != SpecialType.System_Object)
                {
                    string targetTypeName = targetType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
                    resolvedExpr = $"(({targetTypeName}){lookup}!)";
                }
                else
                {
                    resolvedExpr = lookup;
                }
                return true;
            }

            if (inner.StartsWith("StaticResource ", StringComparison.Ordinal))
            {
                string keyRaw = inner.Substring("StaticResource ".Length).Trim();
                if (!TryResolveMarkupOrLiteralKeyExpr(keyRaw, el, ctx, out string keyExpr))
                {
                    return false;
                }
                resolvedExpr = EmitStaticResourceLookupExpr(keyRaw, keyExpr, targetType, ctx);
                return true;
            }

            if (inner.StartsWith("ComponentResourceKey ", StringComparison.Ordinal))
            {
                return TryBuildComponentResourceKeyExpr(inner, el, ctx, out resolvedExpr);
            }

            if (inner.Equals("RelativeSource", StringComparison.Ordinal) || inner.StartsWith("RelativeSource ", StringComparison.Ordinal))
            {
                return TryBuildRelativeSourceExpr(inner, el, ctx, out resolvedExpr);
            }

            if (inner.Equals("Binding", StringComparison.Ordinal) || inner.StartsWith("Binding ", StringComparison.Ordinal))
            {
                return TryBuildBindingExpression(inner, el, ctx, out resolvedExpr);
            }

            return false;
        }

        private static string EmitStaticResourceLookupExpr(string rawKey, string keyExpr, ITypeSymbol? expectedType, XamlCompileContext ctx)
        {
            string typeArg = expectedType is not null
                ? expectedType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)
                : "object";

            bool isLocalOverride = false;
            foreach (HashSet<string> localKeys in ctx.LocalResourceKeyScopes)
            {
                if (localKeys.Contains(rawKey) || localKeys.Contains(NormalizeThemeResourceKey(rawKey)))
                {
                    isLocalOverride = true;
                    break;
                }
            }

            if (!isLocalOverride && ctx.ThemeResourceIndex is not null &&
                ctx.ThemeResourceIndex.TryGetValue(NormalizeThemeResourceKey(rawKey), out var themeEntry))
            {
                string call = themeEntry.TypeName == ctx.CurrentThemeTypeName
                    ? themeEntry.MethodName + "()"
                    : themeEntry.TypeName + "." + themeEntry.MethodName + "()";
                if (typeArg == "object" || typeArg == "object?")
                {
                    return call;
                }
                return $"(({typeArg})(object){call})";
            }

            ctx.NeedsFindResourceHelper = true;
            if (ctx.ResourceScopes.Count == 0)
            {
                return $"__CsuiFindRes<{typeArg}>({keyExpr})";
            }
            string scopes = string.Join(", ", ctx.ResourceScopes.Select(static s => s.Expr));
            return $"__CsuiFindRes<{typeArg}>({keyExpr}, {scopes})";
        }

        private static string NormalizeThemeResourceKey(string rawKey)
        {
            return System.Text.RegularExpressions.Regex.Replace(rawKey.Trim(), @"\s+", " ");
        }

        private static bool TryBuildComponentResourceKeyExpr(string inner, XElement el, XamlCompileContext ctx, out string expr)
        {
            expr = string.Empty;
            string args = inner.Substring("ComponentResourceKey ".Length).Trim();
            string? targetAsmTypeRaw = null;
            string? resourceIdRaw = null;
            foreach (string rawPart in SplitMarkupArgs(args))
            {
                string part = rawPart.Trim();
                if (string.IsNullOrEmpty(part)) continue;
                int eq = part.IndexOf('=');
                if (eq < 0) continue;
                string k = part.Substring(0, eq).Trim();
                string v = UnquoteMarkupArg(part.Substring(eq + 1).Trim());
                if (k == "TypeInTargetAssembly") targetAsmTypeRaw = v;
                else if (k == "ResourceId") resourceIdRaw = v;
            }
            if (string.IsNullOrWhiteSpace(targetAsmTypeRaw) || string.IsNullOrWhiteSpace(resourceIdRaw))
            {
                return false;
            }
            if (!TryResolveTypeAttributeExpr(targetAsmTypeRaw!, el, ctx, out _, out string typeofExpr))
            {
                return false;
            }
            if (!TryResolveMarkupOrLiteralKeyExpr(resourceIdRaw!, el, ctx, out string idExpr))
            {
                return false;
            }
            expr = $"new global::System.Windows.ComponentResourceKey({typeofExpr}, {idExpr})";
            return true;
        }

        private static INamedTypeSymbol? ResolveMarkupType(string typePart, XElement el, XamlCompileContext ctx)
        {
            int colon = typePart.IndexOf(':');
            string xmlNs = el.GetDefaultNamespace().NamespaceName;
            string localType = typePart;
            if (colon >= 0)
            {
                string prefix = typePart.Substring(0, colon);
                localType = typePart.Substring(colon + 1);
                XNamespace? resolvedNs = el.GetNamespaceOfPrefix(prefix);
                if (resolvedNs is null)
                {
                    return null;
                }
                xmlNs = resolvedNs.NamespaceName;
            }
            return ResolveElementType(localType, xmlNs, ctx.ClassNamespace, ctx.Compilation);
        }

        private static bool TryBuildRelativeSourceExpr(string inner, XElement el, XamlCompileContext ctx, out string expr)
        {
            expr = string.Empty;
            string args = inner.Length > "RelativeSource".Length ? inner.Substring("RelativeSource".Length).Trim() : string.Empty;
            if (string.IsNullOrEmpty(args))
            {
                expr = "global::System.Windows.Data.RelativeSource.Self";
                return true;
            }

            string? mode = null;
            string? ancestorTypeRaw = null;
            int? ancestorLevel = null;
            foreach (string rawPart in SplitMarkupArgs(args))
            {
                string part = rawPart.Trim();
                if (string.IsNullOrEmpty(part)) continue;
                int eq = part.IndexOf('=');
                if (eq < 0)
                {
                    mode ??= part;
                    continue;
                }
                string k = part.Substring(0, eq).Trim();
                string v = UnquoteMarkupArg(part.Substring(eq + 1).Trim());
                switch (k)
                {
                    case "Mode":
                        mode = v;
                        break;
                    case "AncestorType":
                        ancestorTypeRaw = v;
                        break;
                    case "AncestorLevel":
                        if (!int.TryParse(v, System.Globalization.NumberStyles.Integer, System.Globalization.CultureInfo.InvariantCulture, out int lvl))
                            return false;
                        ancestorLevel = lvl;
                        break;
                    default:
                        return false;
                }
            }

            mode ??= ancestorTypeRaw is not null ? "FindAncestor" : "Self";
            if (ancestorTypeRaw is not null)
            {
                if (!TryResolveTypeAttributeExpr(ancestorTypeRaw, el, ctx, out _, out string typeofExpr))
                {
                    return false;
                }
                int lvlVal = ancestorLevel ?? 1;
                expr = $"new global::System.Windows.Data.RelativeSource(global::System.Windows.Data.RelativeSourceMode.{mode}, {typeofExpr}, {lvlVal})";
                return true;
            }

            expr = $"new global::System.Windows.Data.RelativeSource(global::System.Windows.Data.RelativeSourceMode.{mode})";
            return true;
        }

        private static string UnquoteMarkupArg(string v)
        {
            if (v.Length >= 2 && ((v[0] == '\'' && v[v.Length - 1] == '\'') || (v[0] == '"' && v[v.Length - 1] == '"')))
            {
                return v.Substring(1, v.Length - 2);
            }
            return v;
        }

        private static bool TryEmitMarkupExtensionAssignment(
            StringBuilder sb,
            string targetExpr,
            INamedTypeSymbol targetType,
            IPropertySymbol prop,
            string expr,
            XElement el,
            XamlCompileContext ctx)
        {
            string inner = expr.Substring(1, expr.Length - 2).Trim();
            if (inner.StartsWith("DynamicResource ", StringComparison.Ordinal))
            {
                ISymbol? dpField = FindDependencyPropertyField(targetType, prop.Name + "Property");
                if (dpField is null)
                {
                    return ctx.Fail($"DynamicResource DP '{targetType.Name}.{prop.Name}Property' not found");
                }
                string keyRaw = inner.Substring("DynamicResource ".Length).Trim();
                if (!TryResolveMarkupOrLiteralKeyExpr(keyRaw, el, ctx, out string keyExpr))
                {
                    return false;
                }
                sb.Append(targetExpr).Append(".SetResourceReference(")
                  .Append(dpField.ContainingType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat))
                  .Append('.').Append(dpField.Name).Append(", ").Append(keyExpr).AppendLine(");");
                return true;
            }

            if (inner.Equals("Binding", StringComparison.Ordinal) || inner.StartsWith("Binding ", StringComparison.Ordinal))
            {
                ISymbol? dpField = FindDependencyPropertyField(targetType, prop.Name + "Property");
                if (dpField is null)
                {
                    // Non-DP Binding properties like DataGridBoundColumn.Binding (type BindingBase) get assigned directly
                    if (!InheritsFrom(prop.Type as INamedTypeSymbol ?? ctx.Compilation.GetSpecialType(SpecialType.System_Object), "System.Windows.Data.BindingBase") &&
                        prop.Type.ToDisplayString() != "System.Windows.Data.BindingBase")
                    {
                        return false;
                    }
                    if (!TryBuildBindingExpression(inner, el, ctx, out string directBindingExpr))
                    {
                        return false;
                    }
                    sb.Append(targetExpr).Append('.').Append(prop.Name).Append(" = ").Append(directBindingExpr).AppendLine(";");
                    return true;
                }

                if (!TryBuildBindingExpression(inner, el, ctx, out string bindingExpr))
                {
                    return false;
                }

                string dpOwner = dpField.ContainingType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
                sb.Append("global::System.Windows.Data.BindingOperations.SetBinding(")
                  .Append(targetExpr).Append(", ")
                  .Append(dpOwner).Append('.').Append(dpField.Name)
                  .Append(", ").Append(bindingExpr).AppendLine(");");
                return true;
            }

            if (!TryResolveMarkupValueExpr(expr, el, prop.Type, ctx, out string valExpr))
            {
                return false;
            }

            sb.Append(targetExpr).Append('.').Append(prop.Name).Append(" = ").Append(valExpr).AppendLine(";");
            return true;
        }

        private static bool TryBuildBindingExpression(string inner, XElement el, XamlCompileContext ctx, out string bindingExpr)
        {
            bindingExpr = string.Empty;
            string args = inner.Length > 7 ? inner.Substring(7).Trim() : string.Empty;
            string? path = null;
            string? mode = null;
            string? updateSourceTrigger = null;
            string? elementName = null;
            string? stringFormat = null;
            string? fallbackValueExpr = null;
            string? targetNullValueExpr = null;
            string? converterExpr = null;
            string? converterParamExpr = null;
            string? relativeSourceExpr = null;
            string? sourceExpr = null;

            if (!string.IsNullOrEmpty(args))
            {
                INamedTypeSymbol? converterType = ctx.Compilation.GetTypeByMetadataName("System.Windows.Data.IValueConverter");
                ITypeSymbol objType = ctx.Compilation.GetSpecialType(SpecialType.System_Object);

                foreach (string rawPart in SplitMarkupArgs(args))
                {
                    string part = rawPart.Trim();
                    if (string.IsNullOrEmpty(part)) continue;
                    int eq = part.IndexOf('=');
                    if (eq < 0)
                    {
                        path ??= UnquoteMarkupArg(part);
                        continue;
                    }
                    string k = part.Substring(0, eq).Trim();
                    string rawV = part.Substring(eq + 1).Trim();
                    string v = UnquoteMarkupArg(rawV);
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
                            if (!TryResolveAttributeValueExpr(rawV, objType, el, ctx, out fallbackValueExpr))
                            {
                                fallbackValueExpr = ToCSharpStringLiteral(v);
                            }
                            break;
                        case "TargetNullValue":
                            if (!TryResolveAttributeValueExpr(rawV, objType, el, ctx, out targetNullValueExpr))
                            {
                                targetNullValueExpr = ToCSharpStringLiteral(v);
                            }
                            break;
                        case "Converter":
                            if (!TryResolveMarkupValueExpr(rawV, el, converterType, ctx, out converterExpr))
                                return false;
                            break;
                        case "ConverterParameter":
                            if (!TryResolveAttributeValueExpr(rawV, objType, el, ctx, out converterParamExpr))
                                return false;
                            break;
                        case "RelativeSource":
                            if (!TryResolveMarkupValueExpr(rawV, el, ctx.Compilation.GetTypeByMetadataName("System.Windows.Data.RelativeSource"), ctx, out relativeSourceExpr))
                                return false;
                            break;
                        case "Source":
                            if (!TryResolveMarkupValueExpr(rawV, el, objType, ctx, out sourceExpr))
                                return false;
                            break;
                        default:
                            return false;
                    }
                }
            }

            var bsb = new StringBuilder();
            var inits = new List<string>();
            if (path is not null && path.IndexOf('(') >= 0)
            {
                bsb.Append("new global::System.Windows.Data.Binding()");
                inits.Add("Path = " + BuildPropertyPathExpr(path, el, ctx));
            }
            else
            {
                bsb.Append("new global::System.Windows.Data.Binding(");
                if (path is not null)
                {
                    bsb.Append(ToCSharpStringLiteral(path));
                }
                bsb.Append(')');
            }
            if (mode is not null) inits.Add("Mode = global::System.Windows.Data.BindingMode." + mode);
            if (updateSourceTrigger is not null) inits.Add("UpdateSourceTrigger = global::System.Windows.Data.UpdateSourceTrigger." + updateSourceTrigger);
            if (elementName is not null) inits.Add("ElementName = " + ToCSharpStringLiteral(elementName));
            if (stringFormat is not null) inits.Add("StringFormat = " + ToCSharpStringLiteral(stringFormat));
            if (fallbackValueExpr is not null) inits.Add("FallbackValue = " + fallbackValueExpr);
            if (targetNullValueExpr is not null) inits.Add("TargetNullValue = " + targetNullValueExpr);
            if (converterExpr is not null) inits.Add("Converter = " + converterExpr);
            if (converterParamExpr is not null) inits.Add("ConverterParameter = " + converterParamExpr);
            if (relativeSourceExpr is not null) inits.Add("RelativeSource = " + relativeSourceExpr);
            if (sourceExpr is not null) inits.Add("Source = " + sourceExpr);

            if (inits.Count > 0)
            {
                bsb.Append(" { ").Append(string.Join(", ", inits)).Append(" }");
            }

            bindingExpr = bsb.ToString();
            return true;
        }

        private static List<string> SplitMarkupArgs(string args)
        {
            var result = new List<string>();
            int start = 0;
            bool inSingle = false;
            bool inDouble = false;
            int braceDepth = 0;
            for (int i = 0; i < args.Length; i++)
            {
                char c = args[i];
                if (c == '\'' && !inDouble) inSingle = !inSingle;
                else if (c == '"' && !inSingle) inDouble = !inDouble;
                else if (!inSingle && !inDouble)
                {
                    if (c == '{') braceDepth++;
                    else if (c == '}' && braceDepth > 0) braceDepth--;
                    else if (c == ',' && braceDepth == 0)
                    {
                        result.Add(args.Substring(start, i - start));
                        start = i + 1;
                    }
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

        private static ISymbol? FindDependencyPropertyField(INamedTypeSymbol type, string fieldName)
        {
            for (INamedTypeSymbol? cur = type; cur is not null; cur = cur.BaseType)
            {
                foreach (ISymbol member in cur.GetMembers(fieldName))
                {
                    if (member is IFieldSymbol fld && fld.IsStatic && fld.DeclaredAccessibility == Accessibility.Public)
                    {
                        return fld;
                    }
                    if (member is IPropertySymbol prop && prop.IsStatic && prop.DeclaredAccessibility == Accessibility.Public)
                    {
                        return prop;
                    }
                }
            }
            return null;
        }

        private static ITypeSymbol GetMemberType(ISymbol member) => member switch
        {
            IFieldSymbol f => f.Type,
            IPropertySymbol p => p.Type,
            _ => throw new InvalidOperationException()
        };

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
            string selfName = type.ToDisplayString();
            if (selfName == "System.Collections.IList" ||
                (type is INamedTypeSymbol nsSelf && nsSelf.OriginalDefinition.ToDisplayString() == "System.Collections.Generic.ICollection<T>"))
            {
                return true;
            }
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

        private static bool TryConvertLiteralToCSharp(
            string raw,
            ITypeSymbol targetType,
            XElement el,
            string classNamespace,
            Compilation compilation,
            out string expr)
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
            if (targetType.SpecialType == SpecialType.System_Object || targetType.SpecialType == SpecialType.System_String)
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
                    return TryParseColorExpr(trimmed, compilation, out expr);
                case "System.Windows.Media.Brush":
                    if (TryParseColorExpr(trimmed, compilation, out string colorExpr))
                    {
                        expr = "new global::System.Windows.Media.SolidColorBrush(" + colorExpr + ")";
                        return true;
                    }
                    return false;
                case "System.Windows.Thickness":
                    if (string.Equals(trimmed, "Auto", StringComparison.OrdinalIgnoreCase))
                    {
                        expr = "new global::System.Windows.Thickness(double.NaN)";
                        return true;
                    }
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
                case "System.Windows.FontStyle":
                    if (string.Equals(trimmed, "Normal", StringComparison.OrdinalIgnoreCase)) { expr = "global::System.Windows.FontStyles.Normal"; return true; }
                    if (string.Equals(trimmed, "Italic", StringComparison.OrdinalIgnoreCase)) { expr = "global::System.Windows.FontStyles.Italic"; return true; }
                    if (string.Equals(trimmed, "Oblique", StringComparison.OrdinalIgnoreCase)) { expr = "global::System.Windows.FontStyles.Oblique"; return true; }
                    return false;
                case "System.Windows.Media.FontFamily":
                    expr = $"new global::System.Windows.Media.FontFamily({ToCSharpStringLiteral(trimmed)})";
                    return true;
                case "System.Windows.GridLength":
                    return TryConvertGridLengthExpr(trimmed, out expr);
                case "System.Windows.Controls.DataGridLength":
                    if (string.Equals(trimmed, "Auto", StringComparison.OrdinalIgnoreCase))
                    {
                        expr = "global::System.Windows.Controls.DataGridLength.Auto";
                        return true;
                    }
                    if (string.Equals(trimmed, "SizeToCells", StringComparison.OrdinalIgnoreCase))
                    {
                        expr = "global::System.Windows.Controls.DataGridLength.SizeToCells";
                        return true;
                    }
                    if (string.Equals(trimmed, "SizeToHeader", StringComparison.OrdinalIgnoreCase))
                    {
                        expr = "global::System.Windows.Controls.DataGridLength.SizeToHeader";
                        return true;
                    }
                    if (trimmed.EndsWith("*", StringComparison.Ordinal))
                    {
                        string numStr = trimmed.Substring(0, trimmed.Length - 1).Trim();
                        double stars = string.IsNullOrEmpty(numStr) ? 1.0 :
                            double.TryParse(numStr, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out double s) ? s : double.NaN;
                        if (double.IsNaN(stars)) return false;
                        expr = $"new global::System.Windows.Controls.DataGridLength({stars.ToString("R", System.Globalization.CultureInfo.InvariantCulture)}d, global::System.Windows.Controls.DataGridLengthUnitType.Star)";
                        return true;
                    }
                    if (double.TryParse(trimmed, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out double dgx))
                    {
                        expr = $"new global::System.Windows.Controls.DataGridLength({dgx.ToString("R", System.Globalization.CultureInfo.InvariantCulture)}d)";
                        return true;
                    }
                    return false;
                case "System.Windows.Controls.Ribbon.RibbonControlLength":
                    if (string.Equals(trimmed, "Auto", StringComparison.OrdinalIgnoreCase))
                    {
                        expr = "global::System.Windows.Controls.Ribbon.RibbonControlLength.Auto";
                        return true;
                    }
                    if (trimmed.EndsWith("*", StringComparison.Ordinal))
                    {
                        string numStr = trimmed.Substring(0, trimmed.Length - 1).Trim();
                        double stars = string.IsNullOrEmpty(numStr) ? 1.0 :
                            double.TryParse(numStr, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out double s) ? s : double.NaN;
                        if (double.IsNaN(stars)) return false;
                        expr = $"new global::System.Windows.Controls.Ribbon.RibbonControlLength({stars.ToString("R", System.Globalization.CultureInfo.InvariantCulture)}d, global::System.Windows.Controls.Ribbon.RibbonControlLengthUnitType.Star)";
                        return true;
                    }
                    if (double.TryParse(trimmed, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out double rpx))
                    {
                        expr = $"new global::System.Windows.Controls.Ribbon.RibbonControlLength({rpx.ToString("R", System.Globalization.CultureInfo.InvariantCulture)}d)";
                        return true;
                    }
                    return false;
                case "System.Windows.Input.Cursor":
                    if (compilation.GetTypeByMetadataName("System.Windows.Input.Cursors") is INamedTypeSymbol cursorsType)
                    {
                        foreach (ISymbol m in cursorsType.GetMembers())
                        {
                            if (m is IPropertySymbol cp && cp.IsStatic && string.Equals(cp.Name, trimmed, StringComparison.OrdinalIgnoreCase))
                            {
                                expr = "global::System.Windows.Input.Cursors." + cp.Name;
                                return true;
                            }
                        }
                    }
                    return false;
                case "System.Windows.TextDecorationCollection":
                    if (string.Equals(trimmed, "None", StringComparison.OrdinalIgnoreCase))
                    {
                        expr = "new global::System.Windows.TextDecorationCollection()";
                        return true;
                    }
                    if (compilation.GetTypeByMetadataName("System.Windows.TextDecorations") is INamedTypeSymbol tdType)
                    {
                        foreach (ISymbol m in tdType.GetMembers())
                        {
                            if (m is IPropertySymbol tp && tp.IsStatic && string.Equals(tp.Name, trimmed, StringComparison.OrdinalIgnoreCase))
                            {
                                expr = "global::System.Windows.TextDecorations." + tp.Name;
                                return true;
                            }
                        }
                    }
                    return false;
                case "System.Windows.Input.ICommand":
                {
                    int cmdDot = trimmed.LastIndexOf('.');
                    string[] cmdClasses = cmdDot > 0
                        ? new[] { "System.Windows.Input." + trimmed.Substring(0, cmdDot).Trim() }
                        : new[]
                        {
                            "System.Windows.Input.ApplicationCommands",
                            "System.Windows.Input.NavigationCommands",
                            "System.Windows.Input.EditingCommands",
                            "System.Windows.Input.MediaCommands",
                            "System.Windows.Input.ComponentCommands"
                        };
                    string cmdMember = cmdDot > 0 ? trimmed.Substring(cmdDot + 1).Trim() : trimmed;
                    foreach (string clsName in cmdClasses)
                    {
                        if (compilation.GetTypeByMetadataName(clsName) is INamedTypeSymbol cmdOwner)
                        {
                            foreach (ISymbol m in cmdOwner.GetMembers())
                            {
                                if ((m is IPropertySymbol p && p.IsStatic || m is IFieldSymbol f && f.IsStatic) &&
                                    string.Equals(m.Name, cmdMember, StringComparison.OrdinalIgnoreCase))
                                {
                                    expr = cmdOwner.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat) + "." + m.Name;
                                    return true;
                                }
                            }
                        }
                    }
                    return false;
                }
                case "System.Windows.Media.ImageSource":
                    expr = $"new global::System.Windows.Media.Imaging.BitmapImage(new global::System.Uri({ToCSharpStringLiteral(trimmed)}, global::System.UriKind.RelativeOrAbsolute))";
                    return true;
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
                case "System.Windows.Media.Transform":
                    expr = $"global::System.Windows.Media.Transform.Parse({ToCSharpStringLiteral(trimmed)})";
                    return true;
                case "System.Windows.Media.DoubleCollection":
                    return TryParseDoublesExpr(trimmed, static nums =>
                        "new global::System.Windows.Media.DoubleCollection { " + string.Join(", ", nums.Select(static n => n + "d")) + " }", out expr);
                case "System.Windows.Media.PointCollection":
                    return TryParseDoublesExpr(trimmed, static nums =>
                    {
                        if (nums.Length % 2 != 0) return null;
                        var pts = new List<string>(nums.Length / 2);
                        for (int i = 0; i < nums.Length; i += 2)
                        {
                            pts.Add($"new global::System.Windows.Point({nums[i]}d, {nums[i + 1]}d)");
                        }
                        return "new global::System.Windows.Media.PointCollection { " + string.Join(", ", pts) + " }";
                    }, out expr);
                case "System.Windows.PropertyPath":
                {
                    var ppCtx = new XamlCompileContext(compilation, classNamespace, "http://schemas.microsoft.com/winfx/2006/xaml", false);
                    expr = BuildPropertyPathExpr(trimmed, el, ppCtx);
                    return true;
                }
                case "System.Windows.Media.Animation.KeyTime":
                    if (string.Equals(trimmed, "Uniform", StringComparison.OrdinalIgnoreCase))
                    {
                        expr = "global::System.Windows.Media.Animation.KeyTime.Uniform";
                        return true;
                    }
                    if (string.Equals(trimmed, "Paced", StringComparison.OrdinalIgnoreCase))
                    {
                        expr = "global::System.Windows.Media.Animation.KeyTime.Paced";
                        return true;
                    }
                    if (trimmed.EndsWith("%", StringComparison.Ordinal) &&
                        double.TryParse(trimmed.Substring(0, trimmed.Length - 1), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out double pct))
                    {
                        expr = $"global::System.Windows.Media.Animation.KeyTime.FromPercent({(pct / 100.0).ToString("R", System.Globalization.CultureInfo.InvariantCulture)}d)";
                        return true;
                    }
                    if (TimeSpan.TryParse(trimmed, System.Globalization.CultureInfo.InvariantCulture, out TimeSpan kts))
                    {
                        expr = $"global::System.Windows.Media.Animation.KeyTime.FromTimeSpan(new global::System.TimeSpan({kts.Ticks}L))";
                        return true;
                    }
                    return false;
                case "System.Type":
                {
                    var dummyCtx = new XamlCompileContext(compilation, classNamespace, "http://schemas.microsoft.com/winfx/2006/xaml", false);
                    INamedTypeSymbol? sym = ResolveMarkupType(trimmed, el, dummyCtx);
                    if (sym is null) return false;
                    expr = "typeof(" + sym.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat) + ")";
                    return true;
                }
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
                case "System.Windows.Media.Animation.RepeatBehavior":
                    if (string.Equals(trimmed, "Forever", StringComparison.OrdinalIgnoreCase))
                    {
                        expr = "global::System.Windows.Media.Animation.RepeatBehavior.Forever";
                        return true;
                    }
                    if (trimmed.EndsWith("x", StringComparison.OrdinalIgnoreCase) &&
                        double.TryParse(trimmed.Substring(0, trimmed.Length - 1), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out double rCount))
                    {
                        expr = $"new global::System.Windows.Media.Animation.RepeatBehavior({rCount.ToString("R", System.Globalization.CultureInfo.InvariantCulture)}d)";
                        return true;
                    }
                    if (TimeSpan.TryParse(trimmed, System.Globalization.CultureInfo.InvariantCulture, out TimeSpan rts))
                    {
                        expr = $"new global::System.Windows.Media.Animation.RepeatBehavior(new global::System.TimeSpan({rts.Ticks}L))";
                        return true;
                    }
                    return false;
            }

            return false;
        }

        private static string BuildPropertyPathExpr(string rawPath, XElement el, XamlCompileContext ctx)
        {
            var sbPath = new StringBuilder(rawPath.Length);
            var parameters = new List<string>();
            int pos = 0;
            while (pos < rawPath.Length)
            {
                int open = rawPath.IndexOf('(', pos);
                if (open < 0)
                {
                    sbPath.Append(rawPath, pos, rawPath.Length - pos);
                    break;
                }
                int close = rawPath.IndexOf(')', open + 1);
                if (close < 0)
                {
                    sbPath.Append(rawPath, pos, rawPath.Length - pos);
                    break;
                }

                sbPath.Append(rawPath, pos, open - pos);
                string inner = rawPath.Substring(open + 1, close - open - 1).Trim();
                int dot = inner.LastIndexOf('.');
                string? paramExpr = null;
                if (dot > 0)
                {
                    string ownerPart = inner.Substring(0, dot).Trim();
                    string propName = inner.Substring(dot + 1).Trim();
                    INamedTypeSymbol? ownerSym = ResolveMarkupType(ownerPart, el, ctx);
                    if (ownerSym is not null)
                    {
                        ISymbol? dp = FindDependencyPropertyField(ownerSym, propName + "Property");
                        if (dp is not null)
                        {
                            paramExpr = dp.ContainingType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat) + "." + dp.Name;
                        }
                    }
                }

                if (paramExpr is not null)
                {
                    sbPath.Append('(').Append(parameters.Count).Append(')');
                    parameters.Add(paramExpr);
                }
                else
                {
                    sbPath.Append(rawPath, open, close - open + 1);
                }

                pos = close + 1;
            }

            if (parameters.Count == 0)
            {
                return $"new global::System.Windows.PropertyPath({ToCSharpStringLiteral(rawPath)})";
            }

            return $"new global::System.Windows.PropertyPath({ToCSharpStringLiteral(sbPath.ToString())}, {string.Join(", ", parameters)})";
        }

        private static bool TryParseColorExpr(string trimmed, Compilation compilation, out string colorExpr)
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

            // Dynamically resolve all 141 WPF Colors.* properties from compilation
            INamedTypeSymbol? colorsType = compilation.GetTypeByMetadataName("System.Windows.Media.Colors");
            if (colorsType is not null)
            {
                foreach (ISymbol m in colorsType.GetMembers())
                {
                    if (m is IPropertySymbol cp && cp.IsStatic && cp.DeclaredAccessibility == Accessibility.Public &&
                        string.Equals(cp.Name, trimmed, StringComparison.OrdinalIgnoreCase))
                    {
                        colorExpr = "global::System.Windows.Media.Colors." + cp.Name;
                        return true;
                    }
                }
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

        private static bool IsAero2ThemePath(string? path)
        {
            if (string.IsNullOrEmpty(path)) return false;
            string norm = path!.Replace('\\', '/');
            return norm.IndexOf("/Themes/Aero2/", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static readonly DiagnosticDescriptor Aero2CompileDiagnostic = new DiagnosticDescriptor(
            id: "CSUI9001",
            title: "Aero2 theme entry failed AOT compilation",
            messageFormat: "Aero2 theme entry '{0}' in '{1}' could not be compiled to pure C#: {2}",
            category: "CSUI.Generators",
            defaultSeverity: DiagnosticSeverity.Warning,
            isEnabledByDefault: true);

        private sealed class Aero2ThemeEntryInfo
        {
            public XElement Element = null!;
            public int Index;
            public string FileStem = string.Empty;
            public string FullThemeClass = string.Empty;
            public string MethodName = string.Empty;
            public string ReturnTypeName = "object";
            public bool IsValueType;
            public string RawKey = string.Empty;
            public INamedTypeSymbol? OwnerType;
            public string? RegKeyExpr;
        }

        private static void EmitAero2ThemeOutputs(
            SourceProductionContext spc,
            ImmutableArray<AdditionalText> allXamls,
            Compilation compilation)
        {
            const string XamlNs = "http://schemas.microsoft.com/winfx/2006/xaml";
            var themeFiles = allXamls
                .Where(static f => IsAero2ThemePath(f.Path))
                .OrderBy(static f => Path.GetFileNameWithoutExtension(f.Path), StringComparer.OrdinalIgnoreCase)
                .ToList();
            if (themeFiles.Count == 0) return;

            var themeIndex = new Dictionary<string, (string TypeName, string MethodName)>(StringComparer.Ordinal);
            var parsedFiles = new List<(string FileName, string FileStem, string FullThemeClass, XElement Root, List<Aero2ThemeEntryInfo> Entries)>();

            // Pass 1: parse all Aero2 theme files and index every top-level resource key
            foreach (AdditionalText tf in themeFiles)
            {
                SourceText? st = tf.GetText();
                if (st is null) continue;
                string raw = st.ToString();
                if (string.IsNullOrWhiteSpace(raw)) continue;

                XDocument doc;
                try { doc = XDocument.Parse(raw); }
                catch { continue; }
                XElement? root = doc.Root;
                if (root is null || root.Name.LocalName != "ResourceDictionary") continue;

                string fileName = Path.GetFileName(tf.Path);
                string stem = SanitizeIdentifier(Path.GetFileNameWithoutExtension(tf.Path));
                string fullThemeClass = "global::__CsuiAero2Themes.__CsuiTheme_" + stem;
                var dummyCtx = new XamlCompileContext(compilation, string.Empty, XamlNs, rootIsFrameworkElement: false)
                {
                    HasInstanceClass = false
                };

                var entries = new List<Aero2ThemeEntryInfo>();
                int idx = 0;
                foreach (XElement entryEl in root.Elements())
                {
                    string? rawKeyAttr = entryEl.Attribute(XName.Get("Key", XamlNs))?.Value;
                    string? targetTypeAttr = entryEl.Attribute("TargetType")?.Value;
                    string? keyToken = !string.IsNullOrWhiteSpace(rawKeyAttr) ? rawKeyAttr : targetTypeAttr;
                    if (string.IsNullOrWhiteSpace(keyToken)) continue;

                    string normKey = NormalizeThemeResourceKey(keyToken!);
                    string methodName = "Get_Res_" + idx.ToString(System.Globalization.CultureInfo.InvariantCulture);

                    INamedTypeSymbol? entrySym = ResolveElementType(entryEl.Name.LocalName, entryEl.Name.NamespaceName, string.Empty, compilation);
                    string retType = "object";
                    bool isValType = false;
                    if (entryEl.Name.LocalName == "Thickness")
                    {
                        retType = "global::System.Windows.Thickness";
                        isValType = true;
                    }
                    else if (entryEl.Name.LocalName == "Visibility")
                    {
                        retType = "global::System.Windows.Visibility";
                        isValType = true;
                    }
                    else if (entryEl.Name.LocalName == "Boolean" || entryEl.Name.LocalName == "Double" || entryEl.Name.LocalName == "Int32" || entryEl.Name.LocalName == "String")
                    {
                        retType = entryEl.Name.LocalName == "String" ? "string" :
                                  entryEl.Name.LocalName == "Boolean" ? "bool" :
                                  entryEl.Name.LocalName == "Double" ? "double" : "int";
                        isValType = retType != "string";
                    }
                    else if (entrySym is not null)
                    {
                        retType = entrySym.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
                        isValType = entrySym.IsValueType;
                    }

                    // Determine owner & registration key for SystemResources.RegisterDefaultThemeFactory
                    INamedTypeSymbol? ownerType = null;
                    string? regKeyExpr = null;
                    if (string.IsNullOrWhiteSpace(rawKeyAttr) && entryEl.Name.LocalName == "Style" && !string.IsNullOrWhiteSpace(targetTypeAttr))
                    {
                        if (TryResolveTypeAttributeExpr(targetTypeAttr!, entryEl, dummyCtx, out ownerType, out string typeofExpr))
                        {
                            regKeyExpr = typeofExpr;
                        }
                    }
                    else if (!string.IsNullOrWhiteSpace(rawKeyAttr))
                    {
                        string rk = rawKeyAttr!.Trim();
                        if (rk.StartsWith("{x:Type ", StringComparison.Ordinal) && rk.EndsWith("}", StringComparison.Ordinal))
                        {
                            if (TryResolveTypeAttributeExpr(rk, entryEl, dummyCtx, out ownerType, out string typeofExpr))
                            {
                                regKeyExpr = typeofExpr;
                            }
                        }
                        else if (rk.StartsWith("{x:Static ", StringComparison.Ordinal) && rk.EndsWith("}", StringComparison.Ordinal))
                        {
                            string memberRef = rk.Substring("{x:Static ".Length, rk.Length - "{x:Static ".Length - 1).Trim();
                            int dot = memberRef.LastIndexOf('.');
                            if (dot > 0)
                            {
                                string typePart = memberRef.Substring(0, dot).Trim();
                                string memberName = memberRef.Substring(dot + 1).Trim();
                                ownerType = ResolveMarkupType(typePart, entryEl, dummyCtx);
                                if (ownerType is not null)
                                {
                                    regKeyExpr = ownerType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat) + "." + memberName;
                                }
                            }
                        }
                        else if (rk.StartsWith("{ComponentResourceKey ", StringComparison.Ordinal) && rk.EndsWith("}", StringComparison.Ordinal))
                        {
                            string args = rk.Substring("{ComponentResourceKey ".Length, rk.Length - "{ComponentResourceKey ".Length - 1).Trim();
                            string? targetAsm = null;
                            foreach (string p in SplitMarkupArgs(args))
                            {
                                int eq = p.IndexOf('=');
                                if (eq > 0 && p.Substring(0, eq).Trim() == "TypeInTargetAssembly")
                                {
                                    targetAsm = UnquoteMarkupArg(p.Substring(eq + 1).Trim());
                                }
                            }
                            if (!string.IsNullOrWhiteSpace(targetAsm) &&
                                TryResolveTypeAttributeExpr(targetAsm!, entryEl, dummyCtx, out ownerType, out _) &&
                                TryBuildComponentResourceKeyExpr(rk.Substring(1, rk.Length - 2).Trim(), entryEl, dummyCtx, out string crkExpr))
                            {
                                regKeyExpr = crkExpr;
                                // Map PresentationFramework shared FrameworkElement CRKs (e.g. MenuScrollViewer, SubmenuContent)
                                // to the control owning the theme dictionary so arbitrary FrameworkElements don't root menu templates.
                                if (ownerType is not null && ownerType.ToDisplayString() == "System.Windows.FrameworkElement")
                                {
                                    ownerType = compilation.GetTypeByMetadataName("System.Windows.Controls." + stem) ?? ownerType;
                                }
                            }
                        }
                        else if (!rk.StartsWith("{", StringComparison.Ordinal))
                        {
                            regKeyExpr = ToCSharpStringLiteral(rk);
                            ownerType = ResolveAero2FileOwnerType(compilation, stem);
                        }
                    }

                    themeIndex[normKey] = (fullThemeClass, methodName);
                    entries.Add(new Aero2ThemeEntryInfo
                    {
                        Element = entryEl,
                        Index = idx,
                        FileStem = stem,
                        FullThemeClass = fullThemeClass,
                        MethodName = methodName,
                        ReturnTypeName = retType,
                        IsValueType = isValType,
                        RawKey = keyToken!,
                        OwnerType = ownerType,
                        RegKeyExpr = regKeyExpr
                    });
                    idx++;
                }

                parsedFiles.Add((fileName, stem, fullThemeClass, root, entries));
            }

            // Pass 2: compile each theme file independently and gather registrations per control owner
            var ownerRegistrations = new Dictionary<INamedTypeSymbol, List<(string KeyExpr, string CallExpr)>>(SymbolEqualityComparer.Default);

            foreach (var pf in parsedFiles)
            {
                var sb = new StringBuilder();
                sb.AppendLine("// <auto-generated />");
                sb.AppendLine("#nullable enable");
                sb.AppendLine();
                sb.AppendLine("namespace __CsuiAero2Themes");
                sb.AppendLine("{");
                sb.Append("    internal static class __CsuiTheme_").AppendLine(pf.FileStem);
                sb.AppendLine("    {");

                foreach (Aero2ThemeEntryInfo entry in pf.Entries)
                {
                    var ctx = new XamlCompileContext(compilation, string.Empty, XamlNs, rootIsFrameworkElement: false)
                    {
                        HasInstanceClass = false,
                        ThemeResourceIndex = themeIndex,
                        CurrentThemeTypeName = pf.FullThemeClass
                    };

                    var methodBody = new StringBuilder();
                    var dummyBind = new StringBuilder();
                    if (!TryEmitAnyValueElement(methodBody, dummyBind, entry.Element, ctx, out string valVar) || dummyBind.Length > 0)
                    {
                        spc.ReportDiagnostic(Diagnostic.Create(Aero2CompileDiagnostic, Location.None, entry.RawKey, pf.FileName, ctx.FailureReason ?? (dummyBind.Length > 0 ? "top-level binding" : "unknown")));
                        continue;
                    }

                    string sField = "s_res_" + entry.Index.ToString(System.Globalization.CultureInfo.InvariantCulture);
                    string bMethod = "Build_Res_" + entry.Index.ToString(System.Globalization.CultureInfo.InvariantCulture);
                    sb.Append("        private static ").Append(entry.ReturnTypeName).Append("? ").Append(sField).AppendLine(";");
                    sb.Append("        internal static ").Append(entry.ReturnTypeName).Append(' ').Append(entry.MethodName)
                      .Append("() => ").Append(sField).Append(" ??= ").Append(bMethod).AppendLine("();");
                    sb.Append("        private static ").Append(entry.ReturnTypeName).Append(' ').Append(bMethod).AppendLine("()");
                    sb.AppendLine("        {");
                    using (var r = new StringReader(methodBody.ToString()))
                    {
                        string? line;
                        while ((line = r.ReadLine()) is not null)
                        {
                            sb.Append("            ").AppendLine(line);
                        }
                    }
                    if (!entry.IsValueType)
                    {
                        sb.Append("            if ((object?)").Append(valVar).AppendLine(" is global::System.Windows.Freezable __frz && __frz.CanFreeze) __frz.Freeze();");
                    }
                    if (ctx.NeedsFindResourceHelper)
                    {
                        sb.AppendLine("            static T __CsuiFindRes<T>(object key, params object?[] scopes)");
                        sb.AppendLine("            {");
                        sb.AppendLine("                foreach (object? s in scopes)");
                        sb.AppendLine("                {");
                        sb.AppendLine("                    object? found = s switch");
                        sb.AppendLine("                    {");
                        sb.AppendLine("                        global::System.Windows.FrameworkElement fe => fe.TryFindResource(key),");
                        sb.AppendLine("                        global::System.Windows.ResourceDictionary rd => rd[key],");
                        sb.AppendLine("                        _ => null");
                        sb.AppendLine("                    };");
                        sb.AppendLine("                    if (found is T typed) return typed;");
                        sb.AppendLine("                }");
                        sb.AppendLine("                object? sysRes = global::System.Windows.SystemResources.FindResourceInternal(key);");
                        sb.AppendLine("                if (sysRes is T sysTyped) return sysTyped;");
                        sb.AppendLine("                throw new global::System.InvalidOperationException($\"StaticResource '{key}' was not found.\");");
                        sb.AppendLine("            }");
                    }
                    sb.Append("            return ").Append(valVar).AppendLine(";");
                    sb.AppendLine("        }");
                    sb.AppendLine();

                    if (entry.OwnerType is not null && !string.IsNullOrEmpty(entry.RegKeyExpr))
                    {
                        if (!ownerRegistrations.TryGetValue(entry.OwnerType, out var regList))
                        {
                            regList = new List<(string KeyExpr, string CallExpr)>();
                            ownerRegistrations[entry.OwnerType] = regList;
                        }
                        string callExpr = pf.FullThemeClass + "." + entry.MethodName + "()";
                        regList.Add((entry.RegKeyExpr!, callExpr));
                        if (entry.RegKeyExpr!.Contains("MenuScrollViewer", StringComparison.Ordinal))
                        {
                            foreach (string menuHostName in new[] { "System.Windows.Controls.ContextMenu", "System.Windows.Controls.ToolBar" })
                            {
                                var menuHostSym = compilation.GetTypeByMetadataName(menuHostName);
                                if (menuHostSym is not null && !SymbolEqualityComparer.Default.Equals(menuHostSym, entry.OwnerType))
                                {
                                    if (!ownerRegistrations.TryGetValue(menuHostSym, out var hostList))
                                    {
                                        hostList = new List<(string KeyExpr, string CallExpr)>();
                                        ownerRegistrations[menuHostSym] = hostList;
                                    }
                                    hostList.Add((entry.RegKeyExpr!, callExpr));
                                }
                            }
                        }

                        // Also register on the Aero2 file's own control type so cross-type keys
                        // (e.g. GridView.GridViewScrollViewerStyleKey in ListView.xaml) and unhooked types
                        // (e.g. PresentationUIStyleResources in DocumentViewer.xaml) are registered when the control loads.
                        var fileOwnerSym = ResolveAero2FileOwnerType(compilation, entry.FileStem);
                        if (fileOwnerSym is not null && !SymbolEqualityComparer.Default.Equals(fileOwnerSym, entry.OwnerType))
                        {
                            if (!ownerRegistrations.TryGetValue(fileOwnerSym, out var fileList))
                            {
                                fileList = new List<(string KeyExpr, string CallExpr)>();
                                ownerRegistrations[fileOwnerSym] = fileList;
                            }
                            if (!fileList.Any(r => r.KeyExpr == entry.RegKeyExpr! && r.CallExpr == callExpr))
                            {
                                fileList.Add((entry.RegKeyExpr!, callExpr));
                            }
                        }
                    }
                }

                sb.AppendLine("    }");
                sb.AppendLine("}");
                spc.AddSource($"Aero2.{pf.FileStem}.g.cs", SourceText.From(sb.ToString(), Encoding.UTF8));
            }

            // Pass 3: emit partial class RegisterDefaultTheme() hooks directly onto each control in CSUI.Core
            if (ownerRegistrations.Count > 0)
            {
                var regSb = new StringBuilder();
                regSb.AppendLine("// <auto-generated />");
                regSb.AppendLine("#nullable enable");
                regSb.AppendLine();

                bool anyRegEmitted = false;
                foreach (var kv in ownerRegistrations)
                {
                    INamedTypeSymbol owner = kv.Key;
                    if (!SymbolEqualityComparer.Default.Equals(owner.ContainingAssembly, compilation.Assembly))
                    {
                        continue;
                    }
                    bool hasPartialHook = owner.GetMembers("RegisterDefaultTheme")
                        .OfType<IMethodSymbol>()
                        .Any(static m => m.IsStatic && m.Parameters.Length == 0 && m.IsPartialDefinition);
                    if (!hasPartialHook)
                    {
                        continue;
                    }

                    anyRegEmitted = true;
                    string ns = owner.ContainingNamespace is { IsGlobalNamespace: false }
                        ? owner.ContainingNamespace.ToDisplayString()
                        : string.Empty;
                    string indent = string.IsNullOrEmpty(ns) ? string.Empty : "    ";
                    if (!string.IsNullOrEmpty(ns))
                    {
                        regSb.Append("namespace ").AppendLine(ns);
                        regSb.AppendLine("{");
                    }

                    string acc = owner.DeclaredAccessibility == Accessibility.Public ? "public" : "internal";
                    string mod = owner.IsStatic ? "static " : string.Empty;
                    regSb.Append(indent).Append(acc).Append(' ').Append(mod).Append("partial class ").AppendLine(owner.Name);
                    regSb.Append(indent).AppendLine("{");
                    regSb.Append(indent).AppendLine("    static partial void RegisterDefaultTheme()");
                    regSb.Append(indent).AppendLine("    {");
                    foreach (var reg in kv.Value)
                    {
                        regSb.Append(indent).Append("        global::System.Windows.SystemResources.RegisterDefaultThemeFactory(")
                             .Append(reg.KeyExpr).Append(", static () => ").Append(reg.CallExpr).AppendLine(");");
                    }
                    regSb.Append(indent).AppendLine("    }");
                    regSb.Append(indent).AppendLine("}");
                    if (!string.IsNullOrEmpty(ns))
                    {
                        regSb.AppendLine("}");
                    }
                    regSb.AppendLine();
                }

                if (anyRegEmitted)
                {
                    spc.AddSource("Aero2.Registrations.g.cs", SourceText.From(regSb.ToString(), Encoding.UTF8));
                }
            }
        }

        private static INamedTypeSymbol? ResolveAero2FileOwnerType(Compilation compilation, string stem)
        {
            string[] prefixes =
            {
                "System.Windows.Controls.",
                "System.Windows.Controls.Primitives.",
                "System.Windows.Documents.",
                "System.Windows.Navigation.",
                "System.Windows."
            };
            foreach (string prefix in prefixes)
            {
                INamedTypeSymbol? sym = compilation.GetTypeByMetadataName(prefix + stem);
                if (sym is not null && sym.GetMembers("RegisterDefaultTheme")
                        .OfType<IMethodSymbol>()
                        .Any(static m => m.IsStatic && m.Parameters.Length == 0 && m.IsPartialDefinition))
                {
                    return sym;
                }
            }
            return null;
        }

        private static string SanitizeIdentifier(string raw)
        {
            var sb = new StringBuilder(raw.Length);
            foreach (char c in raw)
            {
                sb.Append(char.IsLetterOrDigit(c) || c == '_' ? c : '_');
            }
            if (sb.Length == 0 || char.IsDigit(sb[0])) sb.Insert(0, '_');
            return sb.ToString();
        }

        private static string? TryGenerateProjectXamlBootstrap(ImmutableArray<AdditionalText> allXamls, Compilation compilation)
        {
            const string XamlNs = "http://schemas.microsoft.com/winfx/2006/xaml";
            string asmName = compilation.AssemblyName ?? "App";
            if (string.Equals(asmName, "CSUI.Core", StringComparison.Ordinal))
            {
                return null;
            }

            var pathPool = new List<string>();
            foreach (AdditionalText f in allXamls)
            {
                if (!string.IsNullOrEmpty(f.Path))
                {
                    pathPool.Add(f.Path);
                }
            }
            foreach (SyntaxTree tree in compilation.SyntaxTrees)
            {
                string p = tree.FilePath;
                if (!string.IsNullOrEmpty(p) &&
                    p.IndexOf("\\obj\\", StringComparison.OrdinalIgnoreCase) < 0 &&
                    p.IndexOf("/obj/", StringComparison.OrdinalIgnoreCase) < 0)
                {
                    pathPool.Add(p);
                }
            }

            string commonRoot = ComputeCommonDirectoryPrefix(pathPool);
            var boundPropNames = new HashSet<string>(StringComparer.Ordinal);
            var componentViews = new List<(string FullType, string RelPath)>();
            var rdEntries = new List<(string MethodName, string Body, bool NeedsFindRes, string RelPath)>();
            string? appXClass = null;

            int rdCounter = 0;
            foreach (AdditionalText xaml in allXamls)
            {
                SourceText? st = xaml.GetText();
                if (st is null) continue;
                string raw = st.ToString();
                if (string.IsNullOrWhiteSpace(raw)) continue;

                XDocument doc;
                try
                {
                    doc = XDocument.Parse(raw);
                }
                catch
                {
                    continue;
                }

                XElement? root = doc.Root;
                if (root is null) continue;

                ExtractBoundPropertyNames(root, boundPropNames);

                string relPath = ToRelativeSlashPath(xaml.Path, commonRoot);
                string? xClass = root.Attribute(XName.Get("Class", XamlNs))?.Value ?? root.Attribute("Class")?.Value;

                if (!string.IsNullOrWhiteSpace(xClass))
                {
                    if (root.Name.LocalName == "Application")
                    {
                        appXClass = xClass;
                    }
                    else
                    {
                        INamedTypeSymbol? sym = compilation.GetTypeByMetadataName(xClass!);
                        if (sym is not null)
                        {
                            if (!sym.IsAbstract && HasPublicParameterlessCtor(sym))
                            {
                                componentViews.Add((sym.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat), relPath));
                            }
                        }
                        else
                        {
                            int lastDot = xClass!.LastIndexOf('.');
                            string clsNs = lastDot > 0 ? xClass.Substring(0, lastDot) : string.Empty;
                            INamedTypeSymbol? inferredBase = ResolveElementType(root.Name.LocalName, root.Name.NamespaceName, clsNs, compilation);
                            if (inferredBase is not null && !inferredBase.IsAbstract && HasPublicParameterlessCtor(inferredBase))
                            {
                                componentViews.Add(("global::" + xClass, relPath));
                            }
                        }
                    }
                }
                else if (root.Name.LocalName == "ResourceDictionary")
                {
                    var rdCtx = new XamlCompileContext(compilation, string.Empty, XamlNs, rootIsFrameworkElement: false);
                    var rdBody = new StringBuilder();
                    var dummyBind = new StringBuilder();
                    if (TryEmitResourcesCollection(rdBody, dummyBind, "__rd", root, rdCtx) && dummyBind.Length == 0)
                    {
                        rdCounter++;
                        rdEntries.Add(("__BuildRd_" + rdCounter.ToString(System.Globalization.CultureInfo.InvariantCulture), rdBody.ToString(), rdCtx.NeedsFindResourceHelper, relPath));
                    }
                }
            }

            bool emitAppMain = false;
            if (!string.IsNullOrWhiteSpace(appXClass) &&
                compilation.GetEntryPoint(default) is null &&
                (compilation.Options.OutputKind == OutputKind.ConsoleApplication || compilation.Options.OutputKind == OutputKind.WindowsApplication))
            {
                emitAppMain = true;
            }

            // Collect zero-attribute INotifyPropertyChanged & bound POCO property registrations in user assembly
            var accessorLines = new List<string>();
            if (compilation.GetTypeByMetadataName("System.ComponentModel.INotifyPropertyChanged") is INamedTypeSymbol inpcSym &&
                compilation.GetTypeByMetadataName("System.Windows.Data.PropertyAccessorRegistry") is not null)
            {
                var candidateTypes = new List<INamedTypeSymbol>();
                CollectCandidateUserTypes(compilation.Assembly.GlobalNamespace, candidateTypes);

                INamedTypeSymbol? appSym = compilation.GetTypeByMetadataName("System.Windows.Application");
                INamedTypeSymbol? depObjSym = compilation.GetTypeByMetadataName("System.Windows.DependencyObject");

                foreach (INamedTypeSymbol t in candidateTypes)
                {
                    if (HasAttribute(t, "CSUI.Markup.ObservableObjectAttribute") ||
                        HasAttribute(t, "CSUI.Markup.BindableAttribute"))
                    {
                        continue;
                    }
                    if (appSym is not null && InheritsFromSymbol(t, appSym))
                    {
                        continue;
                    }

                    bool isInpc = ImplementsInterfaceSymbol(t, inpcSym);
                    bool isDepObj = depObjSym is not null && InheritsFromSymbol(t, depObjSym);
                    if (!isInpc && boundPropNames.Count == 0)
                    {
                        continue;
                    }

                    string fullT = t.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
                    var seenProps = new HashSet<string>(StringComparer.Ordinal);
                    for (INamedTypeSymbol? cur = t; cur is not null && cur.SpecialType != SpecialType.System_Object; cur = cur.BaseType)
                    {
                        bool declaredInApp = SymbolEqualityComparer.Default.Equals(cur.ContainingAssembly, compilation.Assembly);
                        if (isDepObj && !declaredInApp)
                        {
                            break;
                        }

                        foreach (ISymbol m in cur.GetMembers())
                        {
                            if (m is not IPropertySymbol p || p.IsStatic || p.DeclaredAccessibility != Accessibility.Public)
                            {
                                continue;
                            }

                            if (p.IsIndexer)
                            {
                                if (isInpc && p.Parameters.Length == 1 &&
                                    p.GetMethod is not null && p.GetMethod.DeclaredAccessibility == Accessibility.Public &&
                                    !p.ReturnsByRef && !p.ReturnsByRefReadonly &&
                                    IsSafePublicType(p.Type, compilation.Assembly) &&
                                    IsSafePublicType(p.Parameters[0].Type, compilation.Assembly))
                                {
                                    string idxType = p.Parameters[0].Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
                                    string valType = p.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
                                    bool canWrite = !t.IsValueType && p.SetMethod is not null && p.SetMethod.DeclaredAccessibility == Accessibility.Public && !p.SetMethod.IsInitOnly;
                                    if (canWrite)
                                    {
                                        accessorLines.Add($"global::System.Windows.Data.PropertyAccessorRegistry.RegisterIndexer<{fullT}, {idxType}, {valType}>(static (s, idx) => s[idx], static (s, idx, v) => s[idx] = v, {ToCSharpStringLiteral(p.MetadataName)});");
                                    }
                                    else
                                    {
                                        accessorLines.Add($"global::System.Windows.Data.PropertyAccessorRegistry.RegisterIndexer<{fullT}, {idxType}, {valType}>(static (s, idx) => s[idx], null, {ToCSharpStringLiteral(p.MetadataName)});");
                                    }
                                }
                                continue;
                            }

                            if (!seenProps.Add(p.Name))
                            {
                                continue;
                            }

                            if (!isInpc && !boundPropNames.Contains(p.Name))
                            {
                                continue;
                            }

                            if (p.GetMethod is null || p.GetMethod.DeclaredAccessibility != Accessibility.Public ||
                                p.ReturnsByRef || p.ReturnsByRefReadonly ||
                                !IsSafePublicType(p.Type, compilation.Assembly))
                            {
                                continue;
                            }

                            string propType = p.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
                            bool writable = !t.IsValueType && p.SetMethod is not null && p.SetMethod.DeclaredAccessibility == Accessibility.Public && !p.SetMethod.IsInitOnly;
                            if (writable)
                            {
                                accessorLines.Add($"global::System.Windows.Data.PropertyAccessorRegistry.Register<{fullT}, {propType}>({ToCSharpStringLiteral(p.Name)}, static s => s.{p.Name}, static (s, v) => s.{p.Name} = v);");
                            }
                            else
                            {
                                accessorLines.Add($"global::System.Windows.Data.PropertyAccessorRegistry.Register<{fullT}, {propType}>({ToCSharpStringLiteral(p.Name)}, static s => s.{p.Name});");
                            }
                        }
                    }
                }
            }

            if (!emitAppMain && componentViews.Count == 0 && rdEntries.Count == 0 && accessorLines.Count == 0)
            {
                return null;
            }

            var sb = new StringBuilder();
            sb.AppendLine("// <auto-generated />");
            sb.AppendLine("#nullable enable");
            sb.AppendLine("using System;");
            sb.AppendLine("using System.Runtime.CompilerServices;");
            sb.AppendLine();

            if (emitAppMain && !string.IsNullOrWhiteSpace(appXClass))
            {
                int dot = appXClass!.LastIndexOf('.');
                string appNs = dot > 0 ? appXClass.Substring(0, dot) : string.Empty;
                string appName = dot > 0 ? appXClass.Substring(dot + 1) : appXClass;
                if (!string.IsNullOrEmpty(appNs))
                {
                    sb.Append("namespace ").Append(appNs).AppendLine();
                    sb.AppendLine("{");
                    sb.Append("    public partial class ").AppendLine(appName);
                    sb.AppendLine("    {");
                    sb.AppendLine("        [global::System.STAThreadAttribute]");
                    sb.AppendLine("        public static void Main()");
                    sb.AppendLine("        {");
                    sb.Append("            var app = new ").Append(appName).AppendLine("();");
                    sb.AppendLine("            app.InitializeComponent();");
                    sb.AppendLine("            app.Run();");
                    sb.AppendLine("        }");
                    sb.AppendLine("    }");
                    sb.AppendLine("}");
                }
                else
                {
                    sb.Append("public partial class ").AppendLine(appName);
                    sb.AppendLine("{");
                    sb.AppendLine("    [global::System.STAThreadAttribute]");
                    sb.AppendLine("    public static void Main()");
                    sb.AppendLine("    {");
                    sb.Append("        var app = new ").Append(appName).AppendLine("();");
                    sb.AppendLine("        app.InitializeComponent();");
                    sb.AppendLine("        app.Run();");
                    sb.AppendLine("    }");
                    sb.AppendLine("}");
                }
                sb.AppendLine();
            }

            if (componentViews.Count > 0 || rdEntries.Count > 0 || accessorLines.Count > 0)
            {
                sb.AppendLine("namespace __CsuiGeneratedBootstrap");
                sb.AppendLine("{");
                sb.AppendLine("    internal static class __CsuiProjectInitializer");
                sb.AppendLine("    {");
                sb.AppendLine("        [global::System.Runtime.CompilerServices.ModuleInitializer]");
                sb.AppendLine("        internal static void Initialize()");
                sb.AppendLine("        {");

                foreach ((string fullType, string relPath) in componentViews)
                {
                    sb.Append("            global::System.Windows.Application.RegisterComponentFactory(")
                      .Append(ToCSharpStringLiteral(relPath))
                      .Append(", static () => new ")
                      .Append(fullType)
                      .Append("(), static __c => ((")
                      .Append(fullType)
                      .AppendLine(")__c).InitializeComponent());");
                }

                foreach ((string methodName, _, _, string relPath) in rdEntries)
                {
                    string[] uris =
                    {
                        relPath,
                        "/" + relPath,
                        "pack://application:,,,/" + relPath,
                        "/" + asmName + ";component/" + relPath
                    };
                    foreach (string u in uris)
                    {
                        sb.Append("            global::System.Windows.ResourceDictionary.RegisterSource(new global::System.Uri(")
                          .Append(ToCSharpStringLiteral(u))
                          .Append(", global::System.UriKind.RelativeOrAbsolute), ")
                          .Append(methodName)
                          .AppendLine(");");
                    }
                }

                foreach (string acc in accessorLines)
                {
                    sb.Append("            ").AppendLine(acc);
                }

                sb.AppendLine("        }");

                foreach ((string methodName, string body, bool needsFindRes, _) in rdEntries)
                {
                    sb.AppendLine();
                    sb.Append("        private static global::System.Windows.ResourceDictionary ").Append(methodName).AppendLine("()");
                    sb.AppendLine("        {");
                    sb.AppendLine("            var __rd = new global::System.Windows.ResourceDictionary();");
                    using (var r = new StringReader(body))
                    {
                        string? line;
                        while ((line = r.ReadLine()) is not null)
                        {
                            sb.Append("            ").AppendLine(line);
                        }
                    }
                    if (needsFindRes)
                    {
                        sb.AppendLine("            static T __CsuiFindRes<T>(object key, params object?[] scopes)");
                        sb.AppendLine("            {");
                        sb.AppendLine("                foreach (object? s in scopes)");
                        sb.AppendLine("                {");
                        sb.AppendLine("                    object? found = s switch");
                        sb.AppendLine("                    {");
                        sb.AppendLine("                        global::System.Windows.FrameworkElement fe => fe.TryFindResource(key),");
                        sb.AppendLine("                        global::System.Windows.ResourceDictionary rd => rd[key],");
                        sb.AppendLine("                        _ => null");
                        sb.AppendLine("                    };");
                        sb.AppendLine("                    if (found is T typed) return typed;");
                        sb.AppendLine("                }");
                        sb.AppendLine("                object? g = global::System.Windows.Application.Current?.TryFindResource(key);");
                        sb.AppendLine("                if (g is T gt) return gt;");
                        sb.AppendLine("                throw new global::System.InvalidOperationException($\"StaticResource '{key}' was not found.\");");
                        sb.AppendLine("            }");
                    }
                    sb.AppendLine("            return __rd;");
                    sb.AppendLine("        }");
                }

                sb.AppendLine("    }");
                sb.AppendLine("}");
            }

            return sb.ToString();
        }

        private static string ComputeCommonDirectoryPrefix(List<string> paths)
        {
            if (paths.Count == 0) return string.Empty;
            string first = paths[0].Replace('\\', '/');
            int lastSlash = first.LastIndexOf('/');
            string prefix = lastSlash >= 0 ? first.Substring(0, lastSlash + 1) : string.Empty;
            for (int i = 1; i < paths.Count && prefix.Length > 0; i++)
            {
                string cur = paths[i].Replace('\\', '/');
                while (prefix.Length > 0 && !cur.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                {
                    int cut = prefix.TrimEnd('/').LastIndexOf('/');
                    prefix = cut >= 0 ? prefix.Substring(0, cut + 1) : string.Empty;
                }
            }
            return prefix;
        }

        private static string ToRelativeSlashPath(string fullPath, string commonRoot)
        {
            string norm = fullPath.Replace('\\', '/');
            if (!string.IsNullOrEmpty(commonRoot) && norm.StartsWith(commonRoot, StringComparison.OrdinalIgnoreCase))
            {
                return norm.Substring(commonRoot.Length).TrimStart('/');
            }
            return Path.GetFileName(fullPath);
        }

        private static void ExtractBoundPropertyNames(XElement el, HashSet<string> names)
        {
            foreach (XAttribute attr in el.Attributes())
            {
                string local = attr.Name.LocalName;
                string v = attr.Value.Trim();
                if (local == "DisplayMemberPath" || local == "SelectedValuePath")
                {
                    AddPathSegments(v, names);
                    continue;
                }

                if (v.IndexOf("{Binding", StringComparison.Ordinal) >= 0)
                {
                    ExtractBindingPathsRecursive(v, names);
                }
            }

            foreach (XElement child in el.Elements())
            {
                ExtractBoundPropertyNames(child, names);
            }
        }

        private static void ExtractBindingPathsRecursive(string text, HashSet<string> names)
        {
            int idx = 0;
            while ((idx = text.IndexOf("{Binding", idx, StringComparison.Ordinal)) >= 0)
            {
                int depth = 0;
                int end = idx;
                while (end < text.Length)
                {
                    if (text[end] == '{') depth++;
                    else if (text[end] == '}')
                    {
                        depth--;
                        if (depth == 0) break;
                    }
                    end++;
                }

                if (end > idx && end < text.Length)
                {
                    string inner = text.Substring(idx + 1, end - idx - 1).Trim();
                    string args = inner.Length > 7 ? inner.Substring(7).Trim() : string.Empty;
                    foreach (string rawPart in SplitMarkupArgs(args))
                    {
                        string part = rawPart.Trim();
                        if (string.IsNullOrEmpty(part)) continue;
                        int eq = part.IndexOf('=');
                        if (eq < 0)
                        {
                            AddPathSegments(UnquoteMarkupArg(part), names);
                        }
                        else if (string.Equals(part.Substring(0, eq).Trim(), "Path", StringComparison.Ordinal))
                        {
                            AddPathSegments(UnquoteMarkupArg(part.Substring(eq + 1).Trim()), names);
                        }
                    }
                }
                idx = end + 1;
            }
        }

        private static void AddPathSegments(string path, HashSet<string> names)
        {
            if (string.IsNullOrWhiteSpace(path) || path == ".") return;
            foreach (string rawSeg in path.Split('.'))
            {
                string seg = rawSeg.Trim();
                int bracket = seg.IndexOf('[');
                if (bracket >= 0) seg = seg.Substring(0, bracket).Trim();
                if (seg.Length > 0 && (char.IsLetter(seg[0]) || seg[0] == '_'))
                {
                    names.Add(seg);
                }
            }
        }

        private static void CollectCandidateUserTypes(INamespaceSymbol ns, List<INamedTypeSymbol> results)
        {
            foreach (INamedTypeSymbol t in ns.GetTypeMembers())
            {
                CollectCandidateNamedType(t, results);
            }
            foreach (INamespaceSymbol childNs in ns.GetNamespaceMembers())
            {
                CollectCandidateUserTypes(childNs, results);
            }
        }

        private static void CollectCandidateNamedType(INamedTypeSymbol t, List<INamedTypeSymbol> results)
        {
            if (!t.IsImplicitlyDeclared &&
                !t.Name.StartsWith("<", StringComparison.Ordinal) &&
                !t.Name.StartsWith("__Csui", StringComparison.Ordinal) &&
                !t.IsAbstract && !t.IsStatic && !t.IsGenericType && !t.IsRefLikeType &&
                (t.TypeKind == TypeKind.Class || t.TypeKind == TypeKind.Struct) &&
                IsAccessibleInAssembly(t))
            {
                results.Add(t);
            }

            foreach (INamedTypeSymbol nested in t.GetTypeMembers())
            {
                CollectCandidateNamedType(nested, results);
            }
        }

        private static bool IsAccessibleInAssembly(INamedTypeSymbol t)
        {
            for (INamedTypeSymbol? cur = t; cur is not null; cur = cur.ContainingType)
            {
                if (cur.DeclaredAccessibility != Accessibility.Public && cur.DeclaredAccessibility != Accessibility.Internal)
                {
                    return false;
                }
            }
            return true;
        }

        private static bool IsSafePublicType(ITypeSymbol type, IAssemblySymbol currentAssembly)
        {
            if (type.IsRefLikeType || type.TypeKind == TypeKind.Pointer || type.TypeKind == TypeKind.FunctionPointer || type.TypeKind == TypeKind.Dynamic)
            {
                return false;
            }
            if (type is IArrayTypeSymbol arr)
            {
                return IsSafePublicType(arr.ElementType, currentAssembly);
            }
            if (type is INamedTypeSymbol named)
            {
                for (INamedTypeSymbol? cur = named; cur is not null; cur = cur.ContainingType)
                {
                    if (cur.DeclaredAccessibility != Accessibility.Public &&
                        !(SymbolEqualityComparer.Default.Equals(cur.ContainingAssembly, currentAssembly) && cur.DeclaredAccessibility == Accessibility.Internal))
                    {
                        return false;
                    }
                }
                foreach (ITypeSymbol arg in named.TypeArguments)
                {
                    if (!IsSafePublicType(arg, currentAssembly))
                    {
                        return false;
                    }
                }
            }
            return true;
        }

        private static bool HasAttribute(INamedTypeSymbol t, string fullAttrName)
        {
            foreach (AttributeData attr in t.GetAttributes())
            {
                if (attr.AttributeClass?.ToDisplayString() == fullAttrName) return true;
            }
            return false;
        }

        private static bool InheritsFromSymbol(INamedTypeSymbol type, INamedTypeSymbol baseTarget)
        {
            for (INamedTypeSymbol? cur = type; cur is not null; cur = cur.BaseType)
            {
                if (SymbolEqualityComparer.Default.Equals(cur, baseTarget)) return true;
            }
            return false;
        }

        private static bool ImplementsInterfaceSymbol(INamedTypeSymbol type, INamedTypeSymbol ifaceTarget)
        {
            foreach (INamedTypeSymbol iface in type.AllInterfaces)
            {
                if (SymbolEqualityComparer.Default.Equals(iface, ifaceTarget)) return true;
            }
            return false;
        }

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

                if (localName == "Style" || localName == "ControlTemplate" || localName == "DataTemplate" || localName == "ResourceDictionary")
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
