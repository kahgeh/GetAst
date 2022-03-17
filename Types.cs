namespace GetAst
{
    public class PropertyAst
    {
        public string Name { get; set; }
        public string Type { get; set; }
        public bool IsCollection { get; set; }
        public PropertyAst(string name, string type, bool isCollection)
        {
            Name = name;
            Type = type;
            IsCollection = isCollection;
        }
    }

    public class TypeAst
    {
        public string Name { get; set; }
        public List<PropertyAst> Properties { get; set; }
        public TypeAst(string name)
        {
            Name = name;
            Properties = new List<PropertyAst>();
        }
    }

}
