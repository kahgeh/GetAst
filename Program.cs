// See https://aka.ms/new-console-template for more information
using System.Text.Json;
using GetAst;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

static (bool isCollection, string typeName) GetPropertyType(PropertyDeclarationSyntax property)
{
    if (property.Type is GenericNameSyntax genericName)
    {
        if (genericName.Identifier.ToString().Contains("List"))
        {
            return (true, genericName.TypeArgumentList.Arguments[0].ToString());
        }
    }

    if (property.Type is ArrayTypeSyntax arrayType)
    {
        return (true, arrayType.ElementType.ToString());
    }

    return (false, property.Type.ToString());

}

if (args.Length < 1)
{
    Console.WriteLine("expect file path to a source file");
    return;
}
var filePath = args[0];

var sourceCode = "";
using (var sourceFile = File.OpenText(filePath))
{
    sourceCode = await sourceFile.ReadToEndAsync();
}

if (sourceCode.Length == 0)
{
    Console.WriteLine($"{filePath} is empty");
    return;
}

SyntaxTree tree = CSharpSyntaxTree.ParseText(sourceCode);
CompilationUnitSyntax root = tree.GetCompilationUnitRoot();

var types = new List<TypeAst>();
var stringEnumsModifiers = new string[] { "static", "readonly", "const" };
const string quotationMark = "\"";

foreach (var classNode in root.DescendantNodes()
                         .OfType<ClassDeclarationSyntax>())
{
    var type = new TypeAst(classNode.Identifier.ToString());
    foreach (var property in classNode.DescendantNodes().OfType<PropertyDeclarationSyntax>())
    {
        var (isCollection, typeName) = GetPropertyType(property);
        type.Properties.Add(new PropertyAst(property.Identifier.ToString(), typeName, isCollection));
    }

    if (type.Properties.Count == 0)
    {
        var fields = classNode.DescendantNodes()
          .OfType<FieldDeclarationSyntax>()
          .Where(fds =>
          {
              var modifierTexts = fds.Modifiers.Select(m => m.Text);
              return stringEnumsModifiers.Any(e => modifierTexts.Any(m => m == e));
          });

        if (fields == null)
        {
            continue;
        }

        foreach (var stringField in fields)
        {
            var variable = stringField.Declaration.Variables.First();
            var initializer = variable.Initializer;
            if (initializer == null)
            {
                continue;
            }
            var stringValue = initializer.DescendantNodes().OfType<LiteralExpressionSyntax>().SingleOrDefault();
            if (stringValue == null)
            {
                continue;
            }
            type.StringFields.Add(new KeyValuePair<string, string>(
                variable.Identifier.ValueText,
                stringValue.GetText().ToString().Replace(quotationMark, "")));
        }

    }
    types.Add(type);
}
var serializeOptions = new JsonSerializerOptions
{
    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
};
Console.Write(JsonSerializer.Serialize(types, serializeOptions));

