namespace FingTools.NodeEditor
{
    public class GenericToolbarButton
    {
        public string Name { get; set; }
        public int Width { get; set; } = 50;
        public int Height { get; set;}
        public GenericToolbarButton(string name, int width =50, int height=50)
        {
            Name = name;
            Width = width;
            Height = height;
        }

    }

}
