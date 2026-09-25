using System.Collections.Generic;
using UnityEngine;

// Keep the generated alpha intact; rect metadata trims transparent margins only.
public static class QoriArmoryArt
{
    static readonly Dictionary<string,Sprite> sprites=new Dictionary<string,Sprite>();
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void Reset(){foreach(var sprite in sprites.Values)if(sprite!=null)Object.Destroy(sprite);sprites.Clear();}
    public static Sprite Get(string name)
    {
        if(sprites.TryGetValue(name,out var found)&&found!=null)return found;
        var texture=Resources.Load<Texture2D>("Armory/"+name);
        var data=Resources.Load<TextAsset>("Armory/"+name+"Rect");
        if(texture==null||data==null)return null;
        var fields=data.text.Trim().Split(',');
        var rect=new Rect(int.Parse(fields[0]),int.Parse(fields[1]),int.Parse(fields[2]),int.Parse(fields[3]));
        var sprite=Sprite.Create(texture,rect,new Vector2(.5f,.5f),100,0,SpriteMeshType.FullRect);
        sprite.name=name;sprites[name]=sprite;return sprite;
    }
    public static SpriteRenderer Create(string name,Transform parent,float width,int order)
    {
        var obj=new GameObject(name);obj.transform.SetParent(parent,false);
        var renderer=obj.AddComponent<SpriteRenderer>();renderer.sprite=Get(name);renderer.sortingOrder=order;
        if(renderer.sprite!=null)obj.transform.localScale=Vector3.one*(width/renderer.sprite.bounds.size.x);
        return renderer;
    }
}
