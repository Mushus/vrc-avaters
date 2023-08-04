public class TextureVariantDefinition
{
    public string name;

    public TextureLayerDefinition[] layers;

    public TextureVariantDefinition(string name, TextureLayerDefinition[] layers)
    {
        this.name = name;
        this.layers = layers;
    }
}