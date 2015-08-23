## Finder Asset の概要
Inspectorで操作可能なGetComponent群 です。

コンポーネントまわりのコード量削減／コンポーネント間の疎結合化／エラー特定に貢献できると思います。

### プロジェクトへの追加方法
「Finder_beta2.unitypackageをUnity上にドロップ」するか、「Finderフォルダをそのままプロジェクトに追加」してください。

### 使い方
Finderを利用したサンプルコード（Client.cs）は以下の通りです。

```c#:Client.cs
using UnityEngine;

public class Client : MonoBehaviour {
  public Finder finder;
  
  void Start () {
    bool found = finder.Require<Camera> (); // 指定したコンポーネントが存在しない場合は Exception or false
    if (!found) {
      // Componentがみつからないときの処理をかく
    }
      
    bool allGreens = finder.Requires (typeof(Source), typeof(Transform));  // 複数のコンポーネントを同時に指定できる
    if (!allGreens) {
      // 上記同様...
    }
  }
  
  void Update () {
    Source source = finder.Get<Source> ();      // 検索条件にあてはまる Sourceコンポーネント を1つ取得
    Source[] sources = finder.Gets<Source> ();  // 検索条件にあてはまる Sourceコンポーネント をすべて取得

    // or

    Source source = finder.GetComponent<Source> ();      // Get<Source>() と同等
    Source[] sources = finder.GetComponents<Source> ();  // Gets<Source>() と同等
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


## 詳細情報

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
* Exception [Not Found] : 検索失敗時 Exception を投げます（ただしDebug時のみ有効）。チェックをはずすと null を返すようになります。
  - Jump Hook : コンソール上の Exception をクリックした先を、Finder呼び出し元 or JumpHookメソッド(後述)にフックする。

### コード上からの利用
次のようにコードからFinderを設定することもできます。検索条件を明示したいときや一時的な検索で利用できます。

```c#:Client.cs
public class Client : MonoBehaviour {
  Finder finder = Finder.Create ();  // new Finder() も可

  void Start() {
    finder
      .ByCurrentScope (this)
      .WithCache ()
      .ExceptionWhenNotFound ()
      .Require <Source> ();
  }

  // ...
}
```

ByScopeモードでは検索の基点（from）を設定しますが、この基点は Getメソッド などを呼ぶ瞬間に変更することができます。
これにより、動的に生成した GameObject から検索したいときなどに利用できます。
なおキャッシュは型のみで判断しているので、併用する場合はご注意ください。

```c#:Client.cs
using UnityEngine;

public class Client : MonoBehaviour {
  public Finder finder;
  GameObject obj;

  void Start () {
    this.obj = new GameObject();
    this.obj.AddComponent<Source> ();
  }
  
  void Update () {
    Source source = finder.Get<Source> (this.obj);      // obj の中から Source を検索
  }
}
```

### JumpHook（任意）
コンソールのエラーからのソースへのジャンプ先を Finder呼び出し元 にしていますが、現在の仕組みがUnityの仕様変更により使用できなくなる可能性があるため、安全策としてJumpHookメソッドによるフックも実装しています。JumpHookメソッドの定義は任意のため、もしエラーをクリックしても Finder呼び出し元 へジャンプできない場合のみ、使用することをオススメします。

以下のメソッドを finder.Get<...>() などのFinderメソッドを呼んでいるクラスに定義すると、少なくともそのファイルまではジャンプできるようになります（行の移動まではできない）。

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
