using UnityEngine;

public static class GameConfigs
{
    //资源管理器 加载模式
    public static FoxGame.Asset.AssetLoadMode LoadAssetMode = FoxGame.Asset.AssetLoadMode.AssetBundler;



#if UNITY_ANDROID
	static string curPlatformName = "android";
#elif UNITY_IPHONE
	static string curPlatformName = "iphone";
#else
    static string curPlatformName = "win";
#endif

    //当前平台名
    public static string CurPlatformName { get { return curPlatformName; } }



    //(该文件夹只能读,打包时被一起写入包内,第一次运行游戏把该文件夹数据拷贝到本地ab包路径下) 
    public static string StreamingAssetABRootPath = Application.streamingAssetsPath + "/" + curPlatformName;
    //streamingasset目录下的manifest文件路径
    public static string StreamingAssetManifestPath = Application.streamingAssetsPath + "/" + curPlatformName + "/" + curPlatformName;

    //游戏资源文件路径
    public static string GameResPath = Application.dataPath + "/GameRes";
    //打包资源的输出文件夹(导出到streamingaseet文件夹下)
    public static string GameResExportPath = Application.streamingAssetsPath + "/" + curPlatformName;









    #region game res path
    private static string assetRoot {
        get {
            if (LoadAssetMode == FoxGame.Asset.AssetLoadMode.Editor) { 
                return GameResPath;
            } else {
                return StreamingAssetABRootPath;
            }
        }
    }

    //ui预制体路径
    public static string GetUIPath(string prefabName) {
        string str = "/Prefabs/UI/" + prefabName;
        if (LoadAssetMode != FoxGame.Asset.AssetLoadMode.Editor) {
            str = str.ToLower();
        } else {
            str = str + ".prefab";
        }
        return assetRoot + str;
    }

    //图集路径
    public static string GetSpriteAtlasPath(string name) {
        string str = "/Atlas/" + name;
        if (LoadAssetMode != FoxGame.Asset.AssetLoadMode.Editor) {
            str = str.ToLower();
        } else {
            str = str + ".spriteatlas";
        }
        return assetRoot + str;
    }

    // todo:  扩展...
    
    #endregion
}
