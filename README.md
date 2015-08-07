## FinderAsset

### 概要
簡単にいうと Inspectorで操作可能なGetComponent です。

コード量削減／仕様変更への耐性向上／原因特定のサポートに貢献できると思います。

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

### メリット
* null チェックなどが不要となる
* Missingなどによりコンポーネントを見失ったときエラーの把握が容易になる
* データやオブジェクトの配置が変わった場合でもコードの修正が不要になる
* 比較的安全にコンポーネント間を疎結合にできる

など

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
