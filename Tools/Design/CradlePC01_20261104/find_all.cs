UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/Scenes/Game/MRAtlas_MR01_Cradle.unity");
string Hier(Transform t){string a=t.name;while(t.parent!=null){t=t.parent;a=t.name+"/"+a;}return a;}
return UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Renderer>(true)).Where(r=>r.enabled&&r.gameObject.activeInHierarchy&&r.bounds.min.x<60.3f&&r.bounds.max.x>59f&&r.bounds.min.y<27.5f&&r.bounds.max.y>26f).Select(r=>new{path=Hier(r.transform),type=r.GetType().Name,bounds=new[]{r.bounds.min.x,r.bounds.max.x,r.bounds.min.y,r.bounds.max.y},r.sortingOrder}).ToArray();

