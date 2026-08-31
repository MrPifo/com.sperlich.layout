using UnityEditor;
using UnityEngine;

namespace Sperlich.UISystem.Editor {

	/// <summary>
	/// Weist den MonoScripts des Layout-Pakets automatisch die passenden Unity-Standard-Icons zu,
	/// sodass sie im Inspector-Header und im Projektfenster mit echten Layout-Icons angezeigt werden.
	/// </summary>
	[InitializeOnLoad]
	internal static class LayoutScriptIcons {

		static LayoutScriptIcons() {
			EditorApplication.delayCall += AssignIcons;
		}

		/// <summary>
		/// Weist den Skripten FlexContainer, GridContainer und FlexElement die Unity-Icons zu.
		/// </summary>
		private static void AssignIcons() {
			AssignIconToScript<FlexContainer>("GridLayoutGroup Icon");
			AssignIconToScript<GridContainer>("GridLayoutGroup Icon");
			AssignIconToScript<FlexElement>("LayoutElement Icon");
		}

		/// <summary>
		/// Sucht das MonoScript zum angegebenen Typen und setzt das angegebene Icon.
		/// </summary>
		private static void AssignIconToScript<T>(string iconName) where T : MonoBehaviour {
			Texture2D icon = EditorGUIUtility.IconContent(iconName)?.image as Texture2D;
			if (icon == null) {
				return;
			}

			string[] guids = AssetDatabase.FindAssets($"t:MonoScript {typeof(T).Name}");
			foreach (string guid in guids) {
				string path = AssetDatabase.GUIDToAssetPath(guid);
				var monoScript = AssetDatabase.LoadAssetAtPath<MonoScript>(path);
				if (monoScript != null && monoScript.GetClass() == typeof(T)) {
					var currentIcon = EditorGUIUtility.GetIconForObject(monoScript);
					if (currentIcon != icon) {
						var importer = AssetImporter.GetAtPath(path) as MonoImporter;
						if (importer != null) {
							importer.SetIcon(icon);
							importer.SaveAndReimport();
						}
					}
					break;
				}
			}
		}
	}
}
