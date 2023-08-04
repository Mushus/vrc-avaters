

public class Config
{
    public static PartDefinition body = new PartDefinition()
    {
        textures = new TextureDefinition[]
        {
            new TextureDefinition(new TextureVariantDefinition[]
                {
                    new TextureVariantDefinition("デフォルト", new TextureLayerDefinition[]
                    {
                        new TextureLayerDefinition()
                    }),
                })
        },
    };

    public static PartDefinition head = new PartDefinition()
    {
        textures = new TextureDefinition[]
        {
            new TextureDefinition(new TextureVariantDefinition[]
                {
                    new TextureVariantDefinition("デフォルト", new TextureLayerDefinition[]
                    {
                        new TextureLayerDefinition()
                    }),
                })
        },
    };

    public static PartDefinition get(string key)
    {
        switch (key)
        {
            case "Body":
                return body;
            case "Head":
                return head;
            default:
                return null;
        }
    }
}