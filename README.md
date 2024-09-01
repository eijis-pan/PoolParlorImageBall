PoolParlorを改変したものなので、PoolParlorと同等に使用してもらって構いません。
#

1. PoolParlorImageBall.unitypackage をインポートする
2. Assets/eijis/Prefab/ImageBallManager をヒエラルキーにドロップする
 　（バンク対応イメージボールの場合 ImageBallManager_bank をしようしてください）
4. ヒエラルキーにドロップしたImageBallManagerのインスペクターでImage Ball Manager (U# Script)コンポーネントのTableプロパティにPool Parlor Table/BilliardsModuleを設定する

<img width="778" alt="PoolParlorImageBall" src="https://github.com/user-attachments/assets/2823df70-e0af-4172-a263-d4c696b26807">

#

通常のイメージボール（バンク対応ではない）の場合  
Image Ball In Mirror Parent はバンク対応用の設定プロパティなので空（None）のままで良いです。

※バンク対応イメージボールとは、ガイドラインがクッションで反射するようにしたもののことです

制限事項
- バンク対応イメージボールで反射したガイドラインが表示されるのは最初の1個だけです

#

PoolParlor1.0.3のようにテーブルやボールのサイズが変わらない台では Table Param Polling Interval を0にしてください。  

<img width="577" alt="PoolParlorImageBall_tppi0" src="https://github.com/user-attachments/assets/32ceb648-aaa4-4e9e-bada-4a80fbee9fc5">

#

イメージボールの切り替えスイッチを作る場合は、ImageBallManagerを丸ごとOnOffするのではなく、その中のImageBallをOnOffするようにしてください。  
（説明画像のスイッチは、Pretty Button Pack 1.3 (スイッチ)　https://booth.pm/ja/items/3480091 です。）

<img width="711" alt="ImageBallToggle" src="https://github.com/user-attachments/assets/62070fc5-ca6f-43ff-8a9e-b3b65d765baf">
