using System;
using UnityEngine;
using UnityEditor;

namespace EijisPoolParlorTableUtil
{
	public class PoolParlorImageBallPackage
	{
		private static readonly string exportPackageFilePath = "PoolParlorImageBall_20240905.unityPackage";
		static readonly string[] exportFilePaths = 
		{
			"Assets/eijis/Materials/PoolParlorImageBall/ForrowGuideline.mat",
			"Assets/eijis/Materials/PoolParlorImageBall/ForrowGuideline_bank.mat",
			"Assets/eijis/Materials/PoolParlorImageBall/ImageBallMarker.mat",
			"Assets/eijis/Materials/PoolParlorImageBall/ImageBallMarker_bank.mat",
			"Assets/eijis/Materials/PoolParlorImageBall/ImageBallShadow.mat",
			"Assets/eijis/Materials/PoolParlorImageBall/ImageBallShadow_bank.mat",
			"Assets/eijis/Materials/PoolParlorImageBall/TargetGuideline.mat",
			"Assets/eijis/Materials/PoolParlorImageBall/TargetGuideline_bank.mat",
			"Assets/eijis/Prefab/ImageBall.prefab",
			"Assets/eijis/Prefab/ImageBallManager.prefab",
			"Assets/eijis/Prefab/ImageBallManager_bank.prefab",
			"Assets/eijis/Textures/PoolParlorImageBall/BallShadow_x2.png",
			"Assets/eijis/Textures/PoolParlorImageBall/ImageBallShadow.png",
			"Assets/eijis/UdonScripts/PoolParlorImageBall/ImageBallManager.asset",
			"Assets/eijis/UdonScripts/PoolParlorImageBall/ImageBallManager.cs",
			"Assets/eijis/UdonScripts/PoolParlorImageBall/ImageBallRepositioner.asset",
			"Assets/eijis/UdonScripts/PoolParlorImageBall/ImageBallRepositioner.cs"
		};
		
		[MenuItem("GameObject/TKCH/PoolParlor/ImageBallExportPackage_20240905", false, 0)]
		private static void ExportPackage_Menu(MenuCommand command)
		{
			try
			{
				Debug.Log("ExportPackage");

				AssetDatabase.ExportPackage(exportFilePaths, exportPackageFilePath, ExportPackageOptions.Default);

				EditorUtility.DisplayDialog ("Custom Script Result", "ExportPackage end", "OK");
			}
			catch (Exception ex)
			{
				EditorUtility.DisplayDialog ("Custom Script Exception", ex.ToString(), "OK");
			}
		}
	}
}
