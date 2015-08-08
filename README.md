## Finder Asset

### 概要
簡単にいうと Inspectorで操作可能なGetComponent群 です。

コンポーネントまわりのコード量削減／仕様変更への耐性向上／エラー原因特定のサポートに貢献できると思います。

### プロジェクトへの追加方法
「Finder_beta1.unitypackageをUnity上にドロップ」するか、「Finderフォルダをそのままプロジェクトに追加」してください。

### 使い方
Finderを利用したサンプルコード（Client.cs）は以下の通りです。

```c#:Client.cs
using UnityEngine;

public class Client : MonoBehaviour {
  public Finder finder;
  
  void Start () {
    bool allGreen = finder.Require<Camera> (); // 指定したコンポーネントが存在しない場合は Exception or false
    if (!allGreen) {
      // Componentがみつからないときの処理をかく
    }
      
    bool allGreens = finder.Requires (typeof(Camera), typeof(Transform));  // 複数のコンポーネントを同時に指定できる
    if (!allGreens) {
      // 上記同様...
    }
  }
  
  void Update () {
    Source source = finder.Get<Source> ();      // 検索条件にあてはまる Sourceコンポーネント を1つ取得
    Source[] sources = finder.Gets<Source> ();  // 検索条件にあてはまる Sourceコンポーネント をすべて取得
  }
}
```

Clientスクリプトをオブジェクトにアタッチすると、Inspectorから検索条件を設定できるようになり、その検索条件に応じたコンポーネントを Get(), Gets() できます。また Require(), Requires() で必要なコンポーネントを明示・依存関係の確認ができます。

コンソールの Error Pause と併用して利用することをオススメします。

### メリット
* null チェックなどが不要
* Missingなどによりコンポーネントを見失ったときでも、エラー把握が容易になる
* データやオブジェクトの配置が変わった場合でも、コードを修正せずに依存関係を解消できる
* 複数のコンポーネントを1つのフィールドから型セーフで取得できる
* 比較的安全にコンポーネント間を疎結合にできる

など

### 指定可能な検索条件
Mode

* NullAlways : 常に null を返すようになります。主にデバッグ用です。
* ByScope : from:Component から scope の中を検索します。Inspectorを開いたとき from:Component == null であれば、自動で this(Finderの利用しているスクリプトのコンポーネント) が割り当てられます。
* ByReferenceComponents : 指定した Component[] の中から検索します。
* ByReferenceGameObjects : 指定した GameObject[] の中から検索します。
* ByName : 指定した name:String のゲームオブジェクト内から検索します。
* ByTag : 指定したタグを持つゲームオブジェクト内から検索します。

Option

* cache : 検索結果をキャッシュします。
* Exception [Not Found] : 検索失敗時 Exception を投げます。チェックをはずすと null を返すようになります。
  - Jump Hook : コンソール上の Exception をクリックした先を、Finder呼び出し元 or JumpHookメソッド(後述)にフックする。

### JumpHook（任意）
コンソールのエラーからのソースへのジャンプ先を Finder呼び出し元 にしていますが、現在の仕組みがUnityの仕様変更により使用できなくなる可能性があるため、安全策としてJumpHookメソッドによるフックも実装しています。JumpHookメソッドの定義は任意のため、もしエラーをクリックしても Finder呼び出し元 へジャンプできない場合のみ、使用することをオススメします。

以下のメソッドを finder.Get<...>() などFinderのメソッドを呼んでいるクラスに定義すると、少なくともそのファイルまではジャンプできるようになります（行の移動まではできない）。

```c#:Client.cs
public class Client : MonoBehaviour {
  // JumpHookメソッド
  static System.Exception JumpHook (string message) {
    return new MissingComponentException (message);
  }
  
  public Finder finder;
  // ...
}
```