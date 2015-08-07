## FinderAsser

### プロジェクトへの追加方法
Finder.unitypackageをUnity上にドロップするか、Assets内にあるスクリプトをそのままプロジェクトに追加してください。

### 使い方
Finderを利用したサンプルコード（Client.cs）は以下の通りです。

```c#:Client.cs
using UnityEngine;

public class Client : MonoBehaviour
  public Finder finder;
  
  void Update () {
    Source source = finder.Get<Source> ();      // 検索条件にあてはまる Sourceコンポーネント を1つ取得
    Source[] sources = finder.Gets<Source> ();  // 検索条件にあてはまる Sourceコンポーネント をすべて取得
  }
}
```

Clientスクリプトをオブジェクトにアタッチすると、Inspectorから検索条件を設定できるようになり、その検索条件に応じたコンポーネントを Get(), Gets() できるようになります。

### 指定可能な検索条件
Mode

* NullAlways : 常に null を返すようになります。主にデバッグ用です。
* ByScope : from:Component から scope の中を検索します。
* ByReferenceComponents : 指定した Component[] の中から検索します。
* ByReferenceGameObjects : 指定した GameObject[] の中から検索します。
* ByName : 指定した name:String のゲームオブジェクト内から検索します。
* ByTag : 指定したタグを持つゲームオブジェクト内から検索します。

Option

* cache : 検索結果をキャッシュします。
* Exception When Not Found : 検索失敗時 Exception を投げます。チェックをはずすと null を返すようになります。
