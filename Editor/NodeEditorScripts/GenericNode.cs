using UnityEngine;


namespace FingTools.NodeEditor
{
    public class GenericNode<T>
{
    public string Name { get; set; }
    public Rect Rect { get; set; }
    public T Data { get; set; }

    public GenericNode(string name, Rect rect, T data)
    {
        Name = name;
        Rect = rect;
        Data = data;
    }

   public void UpdatePosition(Vector2 newPosition)
{
    Rect = new Rect(newPosition, Rect.size);
}
}

}
