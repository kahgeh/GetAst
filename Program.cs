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
foreach (var classNode in root.DescendantNodes()
                         .OfType<ClassDeclarationSyntax>())
{
    var type = new TypeAst(classNode.Identifier.ToString());
    foreach (var property in classNode.DescendantNodes().OfType<PropertyDeclarationSyntax>())
    {
        var (isCollection, typeName) = GetPropertyType(property);
        type.Properties.Add(new PropertyAst(property.Identifier.ToString(), typeName, isCollection));
    }
    types.Add(type);
}
var serializeOptions = new JsonSerializerOptions
{
    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
};
Console.Write(JsonSerializer.Serialize(types, serializeOptions));

