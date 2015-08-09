using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using System.Collections.ObjectModel;
using System.Reflection;

[System.Serializable]
public class Finder
{
	public enum FindModes
	{
		NullAlways, ByScope, ByReferenceComponents, ByReferenceGameObjects, ByName, ByTag
	}
	
	public enum Scopes
	{
		Current, Children, Parent, All
	}
	
	[SerializeField] bool isCache = false;
	[SerializeField] FindModes findMode = FindModes.ByScope;
	[SerializeField] bool exceptionWhenNotFound = true;
	
	[SerializeField] Component[] referenceComponents = new Component[1] { null };
	[SerializeField] GameObject[] referenceObjects = new GameObject[1] { null };
	[SerializeField] Scopes scope = Scopes.Current;
	[SerializeField] string name;
	[SerializeField] string tag = "Untagged";
	[SerializeField] Component from;
	[SerializeField] bool isHookJump = true;
	
	readonly Dictionary<System.Type, Component> cache = new Dictionary<System.Type, Component> ();
	readonly Dictionary<System.Type, Component[]> caches = new Dictionary<System.Type, Component[]> ();
	
	/// <summary>
	/// Return and set the search mode of finder.
	/// Clear caches when change the mode.
	/// </summary>
	public FindModes FindMode {
		get { return findMode; }
		private set {
			if (findMode != value)
				ClearCache (); 
			this.findMode = value;
		}
	}
	
	/// <summary>
	/// Cache the instance by Get(),Gets(),... if true.
	/// </summary>
	public bool IsCache {
		get { return isCache; }
		set { this.isCache = value; }
	}
	
	/// <summary>
	/// Throw exception when cannot find the component if true.
	/// </summary>
	public bool IsExceptionWhenNotFound {
		get { return exceptionWhenNotFound; }
		set { this.exceptionWhenNotFound = value; }
	}
	
	/// <summary>
	/// Return and set the root component when search.
	/// Clear caches when change the root.
	/// </summary>
	public Component From {
		get { return from; }
		set { 
			if (from != value)
				ClearCache (); 
			this.from = value;
		}
	}
	
	/// <summary>
	/// Return the scope from root component.
	/// </summary>
	public Scopes Scope {
		get { return scope; }
	}
	
	/// <summary>
	/// Return the name for component search.
	/// </summary>
	public string FindName {
		get { return name; }
	}
	
	/// <summary>
	/// Return the read-only reference components for component search.
	/// </summary>
	public ReadOnlyCollection<Component> ReferenceComponents {
		get { return System.Array.AsReadOnly(referenceComponents); }
	}
	
	/// <summary>
	/// Return the read-only reference GameObjects for component search.
	/// </summary>
	public ReadOnlyCollection<GameObject> ReferenceObjects {
		get { return System.Array.AsReadOnly(referenceObjects); }
	}
	
	/// <summary>
	/// Return a number of cached the component for a component search [GetComponent(), ...]
	/// </summary>
	public int CacheCount {
		get { return cache.Count; } 
	}
	
	/// <summary>
	/// Return number of cached the component for components search [GetComponents(), ...]
	/// </summary>
	public int CachesCount {
		get { return caches.Count; } 
	}
	
	/// <summary>
	/// Return total of cached the component for search.
	/// </summary>
	public int TotalCacheCount {
		get { return cache.Count + caches.Count; } 
	}
	
	bool IsThrowableException {
		get { return Debug.isDebugBuild & exceptionWhenNotFound; }
	}
	
	bool IsDefaultGet  {
		get { return !(IsThrowableException & isHookJump); }
	}
	
	#region for setting
	
	/// <summary>
	/// Clear all the caches for search.
	/// </summary>
	public void ClearCache() {
		cache.Clear ();
		caches.Clear ();
	}
	
	/// <summary>
	/// Change the search mode of finder to 'ByNullAlways'.
	/// </summary>
	public Finder ByNullAlways() {
		this.FindMode = FindModes.NullAlways;
		ClearCache ();
		return this;
	}
	
	/// <summary>
	/// Change the search mode of finder to 'ByScope'.
	/// </summary>
	/// <param name="from">the root component for search</param>
	/// <param name="scope">the scope of search from the root component</param>
	public Finder ByScope(Component from, Scopes scope) {
		if (from == null) {
			if (isHookJump)
				throw JumpHookedException("Argument cannot be null [Parameter name: from]");
			throw new System.ArgumentNullException ("from"); 
		}
		this.FindMode = FindModes.ByScope;
		this.scope = scope;
		this.from = from;
		ClearCache ();
		return this;
	}
	
	/// <summary>
	/// Change the search mode of finder to 'ByReferenceComponents'.
	/// </summary>
	/// <param name="components">the reference components for search</param>
	public Finder ByReferenceComponents(params Component[] components) {
		if (components == null) {
			if (isHookJump)
				throw JumpHookedException("Argument cannot be null [Parameter name: components]");
			throw new System.ArgumentNullException ("components"); 
		}
		this.FindMode = FindModes.ByReferenceComponents;
		this.referenceComponents = components;
		ClearCache ();
		return this;
	}
	
	/// <summary>
	/// Change the search mode of finder to 'ByReferenceGameObjects'.
	/// </summary>
	/// <param name="objects">the reference GameObjects for search</param>
	public Finder ByReferenceGameObjects(Scopes scope, params GameObject[] objects) {
		if (objects == null) {
			if (isHookJump)
				throw JumpHookedException("Argument cannot be null [Parameter name: objects]");
			throw new System.ArgumentNullException ("objects"); 
		}
		this.findMode = FindModes.ByReferenceGameObjects;
		this.scope = scope;
		this.referenceObjects = objects;
		ClearCache ();
		return this;
	}
	
	/// <summary>
	/// Change the search mode of finder to 'ByName'.
	/// </summary>
	/// <param name="name">the name of GameObject that you want to search</param>
	/// <param name="scope">the scope of search from the GameObject</param>
	public Finder ByName(string name, Scopes scope) {
		if (name == null) {
			if (isHookJump)
				throw JumpHookedException("Argument cannot be null [Parameter name: name]");
			throw new System.ArgumentNullException ("name"); 
		}
		this.FindMode = FindModes.ByName;
		this.scope = scope;
		this.name = name;
		ClearCache ();
		return this;
	}
	
	/// <summary>
	/// Change the search mode of finder to 'Tag'.
	/// </summary>
	/// <param name="tag">the tag-name of GameObject that you want to search</param>
	/// <param name="scope">the scope of search from the GameObject</param>
	public Finder ByTag(string tag, Scopes scope) {
		if (tag == null) {
			if (isHookJump)
				throw JumpHookedException("Argument cannot be null [Parameter name: tag]");
			throw new System.ArgumentNullException ("tag"); 
		}
		this.FindMode = FindModes.ByTag;
		this.scope = scope;
		this.tag = tag;
		ClearCache ();
		return this;
	}
	
	/// <summary>
	/// Set enable all found-component caches.
	/// </summary>
	public Finder WithCache () {
		this.IsCache = true;
		return this;
	}
	
	/// <summary>
	/// Set disable all found-component  caches.
	/// </summary>
	public Finder WithoutCache () {
		this.IsCache = false;
		return this;
	}
	
	/// <summary>
	/// When cannot find the component, finder become to throw exception.
	/// </summary>
	public Finder ExceptionWhenNotFound () {
		this.IsExceptionWhenNotFound = true;
		return this;
	}
	
	/// <summary>
	/// When cannot find the component, finder become to return null. Don't throw exception.
	/// </summary>
	public Finder NullWhenNotFound () {
		this.IsExceptionWhenNotFound = false;
		return this;
	}
	
	private UnityException CreateUnityException(string format, params object[] args) {
		return new UnityException (string.Format(format, args));
	}
	
	private MissingComponentException CreateMissingComponentException(string format, params object[] args) {
		return new MissingComponentException (string.Format(format, args));
	}
	
	#endregion
	
	#region hook jumping to code when click error on console 
	
	private System.Exception JumpHookedException(string message) {
		if (Debug.isDebugBuild) {
			var callingType = GetTypeOfCaller ();	
			var jumpHookMethod = callingType.GetMethod ("JumpHook", 
			                                            BindingFlags.NonPublic | BindingFlags.Public |
			                                            BindingFlags.Static);
			
			var hooker = OuterJumpHook (jumpHookMethod, message);
			if (hooker == null)
				hooker = CreateMissingComponentException (message);
			
			var callingFile = GetFileOfCaller ();
			return new MissingComponentException(string.Format ("Called by {0}", callingFile), hooker);
		}
		return new MissingComponentException();
	}
	
	// Maybe, not work in release-build
	private string GetFileOfCaller() {
		var stack = UnityEngine.StackTraceUtility.ExtractStackTrace ().Split ('\n');
		return stack[3];
	}
	
	// Not work in release-build
	private System.Type GetTypeOfCaller() {
		var frame = new System.Diagnostics.StackFrame(3);
		var method = frame.GetMethod ();
		return method.DeclaringType;
	}
	
	#pragma warning disable 168
	private System.Exception OuterJumpHook(MethodInfo method, string message) {
		if (method == null)
			return null;
		
		try {
			return method.Invoke(null, new object[] { message }) as System.Exception;
		} catch (System.Exception ignore) {
			return null;
		}
	}
	#pragma warning restore 168
	
	#endregion
	
	#region Get()
	
	/// <summary>
	/// Find a component of type T:Generics according to the search conditions.
	/// </summary>
	/// <returns>a found component or null</returns>
	/// <exception>
	/// When status of finder is illegal, throw UnityException.
	/// When cannot find component, throw MissingComponentException.
	/// </exception>
	public T GetComponent<T> () where T : Component
	{
		if (from == null) {
			if (isHookJump)
				throw JumpHookedException ("Must set 'from' before find");
			throw CreateUnityException ("Must set 'from' before find");
		}

		if (IsDefaultGet)
			return GetEnter<T> (from.gameObject);
		
		try {
			return GetEnter<T> (from.gameObject);
		} catch (MissingComponentException e) {
			throw JumpHookedException (e.Message);
		}
	}
	
	/// <summary>
	/// As with GetComponent<T>().
	/// </summary>
	public T Get<T> () where T : Component
	{
		if (from == null) {
			if (isHookJump)
				throw JumpHookedException ("Must set 'from' before find");
			throw CreateUnityException ("Must set 'from' before find");
		}

		if (IsDefaultGet)
			return GetEnter<T> (from.gameObject);
		
		try {
			return GetEnter<T> (from.gameObject);
		} catch (MissingComponentException e) {
			throw JumpHookedException (e.Message);
		}
	}
	
	/// <summary>
	/// Find a component of type T:Generics according to the search conditions.
	/// Can specify the root component for search.
	/// </summary>
	/// <param name="from">the root game-object for search</param>
	/// <returns>a found component or null</returns>
	/// <exception>
	/// When status of finder is illegal, throw UnityException.
	/// When cannot find component, throw MissingComponentException.
	/// </exception>
	public T GetComponent<T> (GameObject from) where T : Component
	{
		if (from == null) {
			if (isHookJump)
				throw JumpHookedException("Argument cannot be null [Parameter name: from]");
			throw new System.ArgumentNullException ("from"); 
		}

		if (IsDefaultGet)
			return GetEnter<T> (from);
		
		try {
			return GetEnter<T> (from);
		} catch (MissingComponentException e) {
			throw JumpHookedException (e.Message);
		}
	}
	
	/// <summary>
	/// As with GetComponent<T>(GameObject).
	/// </summary>
	public T Get<T> (GameObject from) where T : Component
	{
		if (from == null) {
			if (isHookJump)
				throw JumpHookedException("Argument cannot be null [Parameter name: from]");
			throw new System.ArgumentNullException ("from"); 
		}

		if (IsDefaultGet)
			return GetEnter<T> (from);
		
		try {
			return GetEnter<T> (from);
		} catch (MissingComponentException e) {
			throw JumpHookedException (e.Message);
		}
	}

	/// <summary>
	/// Find a component of type T:Generics according to the search conditions.
	/// Can specify the root component for search.
	/// </summary>
	/// <param name="from">the root component for search</param>
	/// <returns>a found component or null</returns>
	/// <exception>
	/// When status of finder is illegal, throw UnityException.
	/// When cannot find component, throw MissingComponentException.
	/// </exception>
	public T GetComponent<T> (Component from) where T : Component
	{
		if (from == null) {
			if (isHookJump)
				throw JumpHookedException("Argument cannot be null [Parameter name: from]");
			throw new System.ArgumentNullException ("from"); 
		}
		
		if (IsDefaultGet)
			return GetEnter<T> (from.gameObject);
		
		try {
			return GetEnter<T> (from.gameObject);
		} catch (MissingComponentException e) {
			throw JumpHookedException (e.Message);
		}
	}
	
	/// <summary>
	/// As with GetComponent<T>(Component).
	/// </summary>
	public T Get<T> (Component from) where T : Component
	{
		if (from == null) {
			if (isHookJump)
				throw JumpHookedException("Argument cannot be null [Parameter name: from]");
			throw new System.ArgumentNullException ("from"); 
		}
		
		if (IsDefaultGet)
			return GetEnter<T> (from.gameObject);
		
		try {
			return GetEnter<T> (from.gameObject);
		} catch (MissingComponentException e) {
			throw JumpHookedException (e.Message);
		}
	}

	private T GetEnter<T> (GameObject root) where T : Component
	{
		if (isCache)
			return StaticGet<T> (root);
		return DynamicGet<T> (root, IsThrowableException);
	}
	
	private T StaticGet<T> (GameObject root) where T : Component
	{
		var type = typeof(T);
		
		// return component if component exists in cache
		if (cache.ContainsKey (type))
			return (T) cache [type];
		
		// get component 
		var component = DynamicGet<T> (root, IsThrowableException);
		if (component != null)
			cache [type] = component;
		return component;
	}
	
	private T DynamicGet<T> (GameObject root, bool throwException) where T : Component
	{
		switch (findMode) {
		case FindModes.ByScope:
			return DynamicGetByScope<T> (root, throwException);
			
		case FindModes.ByName:
			return DynamicGetByName<T> (throwException);
			
		case FindModes.ByTag:
			return DynamicGetByTag<T> (throwException);
			
		case FindModes.ByReferenceComponents:
			return DynamicGetByReferenceComponents<T> (throwException);
			
		case FindModes.ByReferenceGameObjects:
			return DynamicGetByReferenceGameObjects<T> (throwException);
			
		case FindModes.NullAlways:
			return null;
			
		default:
			throw new UnityException("Illegal bind-mode");
		}
	}
	
	private T DynamicGetByScope<T> (GameObject root, bool throwException) where T : Component
	{
		var component = FindInScope<T> (root, scope);
		
		if (component == null) {
			if (throwException)
				throw CreateMissingComponentException ("Not found component in {1} of {0}", root, scope);
			return null;
		}
		
		// found
		return component;
	}
	
	private T DynamicGetByName<T> (bool throwException) where T : Component
	{
		// find gameobject
		var obj = GameObject.Find (name);
		
		if (obj == null) {
			if (throwException)
				throw CreateMissingComponentException ("Not found {0} [GameObject]", name);
			return null;
		}
		
		// find component in gameobject
		var component = FindInScope<T> (obj, scope);
		
		if (component == null) {
			if (throwException)
				throw CreateMissingComponentException ("Not found component in {1} of {0} [GameObject]", name, scope);
			return null;
		}
		
		// found
		return component;
	}
	
	private T DynamicGetByTag<T> (bool throwException) where T : Component
	{
		// find gameobject
		var objects = GameObject.FindGameObjectsWithTag (tag);
		
		if (objects.Length == 0) {
			if (throwException)
				throw CreateMissingComponentException ("Not found {0} [GameObject] with tag:{1}", name, tag);
			return null;
		}
		
		// find in reference components
		foreach (var obj in objects) {
			if (obj == null)
				continue;
			var component = FindInScope<T> (obj, Scopes.Current);
			if (component != null)
				return component;	// found
		}
		
		// NOT found
		if (throwException)
			throw CreateMissingComponentException ("Not contains {0} in gameobjects with tag:{2}", typeof(T), tag);
		return null;
	}
	
	private T DynamicGetByReferenceComponents<T> (bool throwException) where T : Component
	{
		// check field
		if (referenceComponents == null)
			throw CreateUnityException ("Must set Reference components");
		
		// find in reference components
		foreach (var component in referenceComponents) {
			var cast = component as T;
			if (cast != null)
				return cast;	// found
		}
		
		// NOT found
		if (throwException)
			throw CreateMissingComponentException ("Not contains {0} in Reference components", typeof(T));
		return null;
	}
	
	private T DynamicGetByReferenceGameObjects<T> (bool throwException) where T : Component
	{
		// check field
		if (referenceObjects == null)
			throw CreateUnityException ("Must set Reference gameobjects");
		
		// find in reference components
		foreach (var obj in referenceObjects) {
			if (obj == null)
				continue;
			var component = FindInScope<T> (obj, scope);
			if (component != null)
				return component;	// found
		}
		
		// NOT found
		if (throwException)
			throw CreateMissingComponentException ("Not contains {0} in {1} of Reference gameobjects", typeof(T), scope);
		return null;
	}
	
	private T FindInScope<T> (GameObject root, Scopes scope) where T : Component
	{
		switch (scope) {
		case Scopes.Current:
			return root.GetComponent<T> ();
			
		case Scopes.Children:
			return root.GetComponentInChildren<T> ();
			
		case Scopes.Parent:
			return root.GetComponentInParent<T> ();
			
		case Scopes.All:
			return Object.FindObjectOfType<T> ();
			
		default:
			throw CreateUnityException ("Scope is illegal");
		}
	}
	
	#endregion
	
	#region Gets()
	
	/// <summary>
	/// Find components of type T:Generics according to the search conditions.
	/// </summary>
	/// <returns>found components or null</returns>
	/// <exception>
	/// When status of finder is illegal, throw UnityException.
	/// When cannot find component, throw MissingComponentException.
	/// </exception>
	public T[] GetComponents<T> () where T : Component
	{
		if (from == null) {
			if (isHookJump)
				throw JumpHookedException ("Must set 'from' before find");
			throw CreateUnityException ("Must set 'from' before find");
		}

		if (IsDefaultGet)
			return GetsEnter<T> (from.gameObject);
		
		try {
			return GetsEnter<T> (from.gameObject);
		} catch (MissingComponentException e) {
			throw JumpHookedException (e.Message);
		}
	}
	
	/// <summary>
	/// As with GetComponents<T>().
	/// </summary>
	public T[] Gets<T> () where T : Component
	{
		if (from == null) {
			if (isHookJump)
				throw JumpHookedException ("Must set 'from' before find");
			throw CreateUnityException ("Must set 'from' before find");
		}

		if (IsDefaultGet)
			return GetsEnter<T> (from.gameObject);
		
		try {
			return GetsEnter<T> (from.gameObject);
		} catch (MissingComponentException e) {
			throw JumpHookedException (e.Message);
		}
	}
	
	/// <summary>
	/// Find components of type T:Generics according to the search conditions.
	/// Can specify the root component for search.
	/// </summary>
	/// <param name="from">the root game-object for search</param>
	/// <returns>found components or null</returns>
	/// <exception>
	/// When status of finder is illegal, throw UnityException.
	/// When cannot find component, throw MissingComponentException.
	/// </exception>
	public T[] GetComponents<T> (GameObject from) where T : Component
	{
		if (from == null) {
			if (isHookJump)
				throw JumpHookedException("Argument cannot be null [Parameter name: from]");
			throw new System.ArgumentNullException ("from"); 
		}

		if (IsDefaultGet)
			return GetsEnter<T> (from);
		
		try {
			return GetsEnter<T> (from);
		} catch (MissingComponentException e) {
			throw JumpHookedException (e.Message);
		}
	}
	
	/// <summary>
	/// As with GetComponents<T>(GameObject).
	/// </summary>
	public T[] Gets<T> (GameObject from) where T : Component
	{
		if (from == null) {
			if (isHookJump)
				throw JumpHookedException("Argument cannot be null [Parameter name: from]");
			throw new System.ArgumentNullException ("from"); 
		}

		if (IsDefaultGet)
			return GetsEnter<T> (from);
		
		try {
			return GetsEnter<T> (from);
		} catch (MissingComponentException e) {
			throw JumpHookedException (e.Message);
		}
	}

	/// <summary>
	/// Find components of type T:Generics according to the search conditions.
	/// Can specify the root component for search.
	/// </summary>
	/// <param name="from">the root component for search</param>
	/// <returns>found components or null</returns>
	/// <exception>
	/// When status of finder is illegal, throw UnityException.
	/// When cannot find component, throw MissingComponentException.
	/// </exception>
	public T[] GetComponents<T> (Component from) where T : Component
	{
		if (from == null) {
			if (isHookJump)
				throw JumpHookedException("Argument cannot be null [Parameter name: from]");
			throw new System.ArgumentNullException ("from"); 
		}
		
		if (IsDefaultGet)
			return GetsEnter<T> (from.gameObject);
		
		try {
			return GetsEnter<T> (from.gameObject);
		} catch (MissingComponentException e) {
			throw JumpHookedException (e.Message);
		}
	}
	
	/// <summary>
	/// As with GetComponents<T>(Component).
	/// </summary>
	public T[] Gets<T> (Component from) where T : Component
	{
		if (from == null) {
			if (isHookJump)
				throw JumpHookedException("Argument cannot be null [Parameter name: from]");
			throw new System.ArgumentNullException ("from"); 
		}
		
		if (IsDefaultGet)
			return GetsEnter<T> (from.gameObject);
		
		try {
			return GetsEnter<T> (from.gameObject);
		} catch (MissingComponentException e) {
			throw JumpHookedException (e.Message);
		}
	}
	
	private T[] GetsEnter<T> (GameObject root) where T : Component
	{
		if (isCache)
			return StaticGets<T> (root);
		return DynamicGets<T> (root, IsThrowableException);
	}
	
	private T[] StaticGets<T> (GameObject root) where T : Component
	{
		var type = typeof(T);
		
		// return component if component exists in cache
		if (caches.ContainsKey (type))
			return (T[]) caches [type];
		
		// get component 
		var components = DynamicGets<T> (root, IsThrowableException);
		if (components != null)
			caches [type] = components;
		return components;
	}
	
	private T[] DynamicGets<T> (GameObject root, bool throwException) where T : Component
	{
		switch (findMode) {
		case FindModes.ByScope:
			return DynamicGetsByScope<T> (root);
			
		case FindModes.ByName:
			return DynamicGetsByName<T> (throwException);
			
		case FindModes.ByTag:
			return DynamicGetsByTag<T> (throwException);
			
		case FindModes.ByReferenceComponents:
			return DynamicGetsByReferenceComponents<T> (throwException);
			
		case FindModes.ByReferenceGameObjects:
			return DynamicGetsByReferenceGameObjects<T> (throwException);
			
		case FindModes.NullAlways:
			return null;
			
		default:
			throw new UnityException("Illegal bind-mode");
		}
	}
	
	private T[] DynamicGetsByScope<T> (GameObject root) where T : Component
	{
		return FindsInScope<T> (root, scope);
	}
	
	private T[] DynamicGetsByName<T> (bool throwException) where T : Component
	{
		// find gameobject
		var obj = GameObject.Find (name);
		
		if (obj == null) {
			if (throwException)
				throw CreateMissingComponentException ("Not found {0} [GameObject]", name);
			return new T [0];
		}
		
		// find component in gameobject
		var components = FindsInScope<T> (obj, scope);
		
		if (components.Length <= 0 && throwException)
			throw CreateMissingComponentException ("Not found component in {1} of {0} [GameObject]", name, scope);
		
		// found or empty
		return components;
	}
	
	private T[] DynamicGetsByTag<T> (bool throwException) where T : Component
	{
		// find gameobject
		var objects = GameObject.FindGameObjectsWithTag (tag);
		
		if (objects.Length == 0) {
			if (throwException)
				throw CreateMissingComponentException ("Not found {0} [GameObject] with tag:{1}", name, tag);
			return null;
		}
		
		// find in reference components
		var components = new List<T>();
		foreach (var obj in objects) {
			if (obj == null)
				continue;
			var founds = FindsInScope<T> (obj, Scopes.Current);
			foreach (var component in founds)
				components.Add (component);		// fou
		}
		
		// NOT found
		if (components.Count <= 0 && throwException)
			throw CreateMissingComponentException ("Not contains {0} in gameobjects with tag:{1}", typeof(T), tag);
		
		// found or empty
		return components.ToArray();
	}
	
	private T[] DynamicGetsByReferenceComponents<T> (bool throwException) where T : Component
	{
		// check field
		if (referenceComponents == null)
			throw CreateUnityException ("Must set Reference components");
		
		// find in reference components
		var components = new List<T>();
		foreach (var component in referenceComponents) {
			var cast = component as T;
			if (cast != null)
				components.Add (cast);	// found
		}
		
		// NOT found
		if (components.Count <= 0 && throwException)
			throw CreateMissingComponentException ("Not contains {0} in Reference components", typeof(T));
		
		// found or empty
		return components.ToArray();
	}
	
	private T[] DynamicGetsByReferenceGameObjects<T> (bool throwException) where T : Component
	{
		// check field
		if (referenceObjects == null)
			throw CreateUnityException ("Must set Reference gameobjects");
		
		// find in reference components
		var components = new HashSet<T>();
		foreach (var obj in referenceObjects) {
			if (obj == null)
				continue;
			var founds = FindsInScope<T> (obj, scope);
			foreach (var component in founds)
				components.Add (component);		// found
		}
		
		// NOT found
		if (components.Count <= 0 && throwException)
			throw CreateMissingComponentException ("Not contains {0} in {1} of Reference gameobjects", typeof(T), scope);
		
		// found or empty
		return components.ToArray();
	}
	
	private T[] FindsInScope<T> (GameObject root, Scopes scope) where T : Component
	{
		switch (scope) {
		case Scopes.Current:
			return root.GetComponents<T> ();
			
		case Scopes.Children:
			return root.GetComponentsInChildren<T> ();
			
		case Scopes.Parent:
			return root.GetComponentsInParent<T> ();
			
		case Scopes.All:
			return Object.FindObjectsOfType<T> ();
			
		default:
			throw CreateUnityException ("Scope is illegal");
		}
	}
	
	#endregion
	
	#region Require(), Requires()
	
	#pragma warning disable 168
	
	/// <summary>
	/// Clarify found component-type, and check to exist the component of type T in the search target.
	/// </summary>
	/// <returns>true if exist the component</returns>
	/// <exception>
	/// When status of finder is illegal, throw UnityException.
	/// When not exist component, throw MissingComponentException.
	/// </exception>
	public bool Require<T> () where T : Component
	{
		if (from == null) {
			if (isHookJump)
				throw JumpHookedException ("Must set 'from' before find");
			throw CreateUnityException ("Must set 'from' before find");
		}

		if (IsDefaultGet) {
			var component = DynamicGet<T> (from.gameObject, IsThrowableException);
			return component != null;
		}
		
		try {
			var component = DynamicGet<T> (from.gameObject, IsThrowableException);
			return component != null;
		} catch (MissingComponentException ignore) {
			throw JumpHookedException (string.Format("Require {0}", typeof(T)));
		}
	}
	
	/// <summary>
	/// Clarify found component-type, and check to exist the component of type T in the search target.
	/// </summary>
	/// <param name="from">the root game-object for search</param>
	/// <returns>true if exist the component</returns>
	/// <exception>
	/// When status of finder is illegal, throw UnityException.
	/// When not exist component, throw MissingComponentException.
	/// </exception>
	public bool Require<T> (GameObject from) where T : Component
	{
		if (from == null) {
			if (isHookJump)
				throw JumpHookedException("Argument cannot be null [Parameter name: from]");
			throw new System.ArgumentNullException ("from"); 
		}

		if (IsDefaultGet) {
			var component = DynamicGet<T> (from, IsThrowableException);
			return component != null;
		}
		
		try {
			var component = DynamicGet<T> (from, IsThrowableException);
			return component != null;
		} catch (MissingComponentException ignore) {
			throw JumpHookedException (string.Format("Require {0}", typeof(T)));
		}
	}

	/// <summary>
	/// Clarify found component-type, and check to exist the component of type T in the search target.
	/// </summary>
	/// <param name="from">the root component for search</param>
	/// <returns>true if exist the component</returns>
	/// <exception>
	/// When status of finder is illegal, throw UnityException.
	/// When not exist component, throw MissingComponentException.
	/// </exception>
	public bool Require<T> (Component from) where T : Component
	{
		if (from == null) {
			if (isHookJump)
				throw JumpHookedException("Argument cannot be null [Parameter name: from]");
			throw new System.ArgumentNullException ("from"); 
		}
		
		if (IsDefaultGet) {
			var component = DynamicGet<T> (from.gameObject, IsThrowableException);
			return component != null;
		}
		
		try {
			var component = DynamicGet<T> (from.gameObject, IsThrowableException);
			return component != null;
		} catch (MissingComponentException ignore) {
			throw JumpHookedException (string.Format("Require {0}", typeof(T)));
		}
	}
	
	#pragma warning restore 168
	
	/// <summary>
	/// Clarify found component-types, and check to exist all components of type T in the search target.
	/// </summary>
	/// <returns>true if exist all the components</returns>
	/// <exception>
	/// When status of finder is illegal, throw UnityException.
	/// When not exist component, throw MissingComponentException.
	/// </exception>
	public bool Requires (params System.Type[] types)
	{ 
		if (from == null) {
			if (isHookJump)
				throw JumpHookedException ("Must set 'from' before find");
			throw CreateUnityException ("Must set 'from' before find");
		}

		var notFound = GetNotFoundComponents (from .gameObject, types);
		
		if (0 < notFound.Count) {
			if (IsThrowableException) {
				var strTypes = notFound
					.Distinct()
						.Select (com => com.ToString())
						.ToArray();
				var join = string.Join (" / ", strTypes);
				
				if (IsDefaultGet)
					throw new MissingComponentException(string.Format("Requires {0}", join));
				
				try {
					throw new MissingComponentException(string.Format("Requires {0}", join));
				} catch (MissingComponentException e) {
					throw JumpHookedException (e.Message);
				}
			}
			return false;
		}
		
		return true;
	}
	
	/// <summary>
	/// Clarify found component-types, and check to exist all components of type T in the search target.
	/// </summary>
	/// <param name="from">the root game-object for search</param>
	/// <returns>true if exist all the components</returns>
	/// <exception>
	/// When status of finder is illegal, throw UnityException.
	/// When not exist component, throw MissingComponentException.
	/// </exception>
	public bool Requires (GameObject from, params System.Type[] types)
	{ 
		if (from == null) {
			if (isHookJump)
				throw JumpHookedException("Argument cannot be null [Parameter name: from]");
			throw new System.ArgumentNullException ("from"); 
		}

		var notFound = GetNotFoundComponents (from, types);
		
		if (0 < notFound.Count) {
			if (IsThrowableException) {
				var strTypes = notFound
					.Distinct()
						.Select (com => com.ToString())
						.ToArray();
				var join = string.Join (" / ", strTypes);
				
				if (IsDefaultGet)
					throw new MissingComponentException(string.Format("Requires {0}", join));
				
				try {
					throw new MissingComponentException(string.Format("Requires {0}", join));
				} catch (MissingComponentException e) {
					throw JumpHookedException (e.Message);
				}
			}
			return false;
		}
		
		return true;
	}

	/// <summary>
	/// Clarify found component-types, and check to exist all components of type T in the search target.
	/// </summary>
	/// <param name="from">the root component for search</param>
	/// <returns>true if exist all the components</returns>
	/// <exception>
	/// When status of finder is illegal, throw UnityException.
	/// When not exist component, throw MissingComponentException.
	/// </exception>
	public bool Requires (Component from, params System.Type[] types)
	{ 
		if (from == null) {
			if (isHookJump)
				throw JumpHookedException("Argument cannot be null [Parameter name: from]");
			throw new System.ArgumentNullException ("from"); 
		}
		
		var notFound = GetNotFoundComponents (from.gameObject, types);
		
		if (0 < notFound.Count) {
			if (IsThrowableException) {
				var strTypes = notFound
					.Distinct()
						.Select (com => com.ToString())
						.ToArray();
				var join = string.Join (" / ", strTypes);
				
				if (IsDefaultGet)
					throw new MissingComponentException(string.Format("Requires {0}", join));
				
				try {
					throw new MissingComponentException(string.Format("Requires {0}", join));
				} catch (MissingComponentException e) {
					throw JumpHookedException (e.Message);
				}
			}
			return false;
		}
		
		return true;
	}
	
	private List<System.Type> GetNotFoundComponents(GameObject from, params System.Type[] types) {
		var notFound = new List<System.Type> ();
		foreach (var type in types) {
			if (type == null)
				continue;
			
			MethodInfo method = typeof(Finder).GetMethod("DynamicGet", BindingFlags.NonPublic | BindingFlags.Instance);
			MethodInfo generic = method.MakeGenericMethod(type);
			var component = generic.Invoke(this, new object[] { from, false });
			if (component == null)
				notFound.Add (type);
		}
		return notFound;
	}
	
	#endregion
}
