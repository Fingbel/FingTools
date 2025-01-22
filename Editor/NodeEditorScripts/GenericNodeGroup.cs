using System.Collections.Generic;


namespace FingTools.NodeEditor
{
    public class GenericNodeGroup<T>
{
    public string Name { get; set; }
    public List<GenericNode<T>> Nodes { get; } = new List<GenericNode<T>>();

    public GenericNodeGroup(string name)
    {
        Name = name;
    }

    public void AddNode(GenericNode<T> node)
    {
        Nodes.Add(node);
    }    
}

}
