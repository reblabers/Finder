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
	
	bool IsDefaultState {
		get { return !(exceptionWhenNotFound & isHookJump); }
	}

	public FindModes FindMode {
		get { return findMode; }
		set { this.findMode = value; }
	}

	public bool IsCache {
		get { return isCache; }
		set { this.isCache = value; }
	}

	public bool ExceptionWhenNotFound {
		get { return exceptionWhenNotFound; }
		set { this.exceptionWhenNotFound = value; }
	}

	public Component From {
		get { return from; }
		set { this.from = value; }
	}

	public Scopes Scope {
		get { return scope; }
	}

	public string FindName {
		get { return name; }
	}

	public ReadOnlyCollection<Component> ReferenceComponents {
		get { return System.Array.AsReadOnly(referenceComponents); }
	}

	public ReadOnlyCollection<GameObject> ReferenceObjects {
		get { return System.Array.AsReadOnly(referenceObjects); }
	}

	public int CacheCount {
		get { return cache.Count; } 
	}

	public int CachesCount {
		get { return caches.Count; } 
	}

	public int TotalCacheCount {
		get { return cache.Count + caches.Count; } 
	}

	public void ClearCache() {
		cache.Clear ();
		caches.Clear ();
	}

	public void ByNullAlways() {
		this.findMode = FindModes.NullAlways;
	}

	public void ByScope(Component root, Scopes scope) {
		this.findMode = FindModes.ByScope;
		this.scope = scope;
		this.from = root;
	}

	public void ByReferenceComponents(params Component[] components) {
		if (components == null)
			throw new System.ArgumentNullException ("components");
		this.findMode = FindModes.ByReferenceComponents;
		this.referenceComponents = components;
	}

	public void ByReferenceGameObjects(Scopes scope, params GameObject[] objects) {
		if (objects == null)
			throw new System.ArgumentNullException ("objects");
		this.findMode = FindModes.ByReferenceGameObjects;
		this.scope = scope;
		this.referenceObjects = objects;
	}

	public void ByScope(string name, Scopes scope) {
		if (name == null)
			throw new System.ArgumentNullException ("name");
		this.findMode = FindModes.ByScope;
		this.scope = scope;
		this.name = name;
	}
	
	private UnityException CreateUnityException(string format, params object[] args) {
		return new UnityException (string.Format(format, args));
	}
	
	private MissingComponentException CreateMissingComponentException(string format, params object[] args) {
		return new MissingComponentException (string.Format(format, args));
	}

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
	
	public T Get<T> () where T : Component
	{
		if (IsDefaultState)
			return GetEnter<T> (from);

		try {
			return GetEnter<T> (from);
		} catch (MissingComponentException e) {
    		throw JumpHookedException (e.Message);
		}
	}

	public T Get<T> (Component root) where T : Component
	{
		if (IsDefaultState)
			return GetEnter<T> (root);

		try {
			return GetEnter<T> (root);
		} catch (MissingComponentException e) {
			throw JumpHookedException (e.Message);
		}
	}

	private T GetEnter<T> (Component root) where T : Component
	{
		if (isCache)
			return StaticGet<T> (root);
		return DynamicGet<T> (root, exceptionWhenNotFound);
	}

	private T StaticGet<T> (Component root) where T : Component
	{
		var type = typeof(T);

		// return component if component exists in cache
		if (cache.ContainsKey (type))
			return (T) cache [type];

		// get component 
		var component = DynamicGet<T> (root, exceptionWhenNotFound);
		if (component != null)
			cache [type] = component;
		return component;
	}
	
	private T DynamicGet<T> (Component root, bool throwException) where T : Component
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

	private T DynamicGetByScope<T> (Component root, bool throwException) where T : Component
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

	private T FindInScope<T> (Component root, Scopes scope) where T : Component
	{
		if (root == null)
			throw CreateUnityException ("Must set 'from' before find");
		return FindInScope<T> (root.gameObject, scope);
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
	
	public T[] Gets<T> () where T : Component
	{
		if (IsDefaultState)
			return GetsEnter<T> (from);

		try {
			return GetsEnter<T> (from);
		} catch (MissingComponentException e) {
			throw JumpHookedException (e.Message);
		}
	}
	
	public T[] Gets<T> (Component root) where T : Component
	{
		if (IsDefaultState)
			return GetsEnter<T> (root);

		try {
			return GetsEnter<T> (root);
		} catch (MissingComponentException e) {
			throw JumpHookedException (e.Message);
		}
	}

	private T[] GetsEnter<T> (Component root) where T : Component
	{
		if (isCache)
			return StaticGets<T> (root);
		return DynamicGets<T> (root, exceptionWhenNotFound);
	}

	private T[] StaticGets<T> (Component root) where T : Component
	{
		var type = typeof(T);

		// return component if component exists in cache
		if (caches.ContainsKey (type))
			return (T[]) caches [type];

		// get component 
		var components = DynamicGets<T> (root, exceptionWhenNotFound);
		if (components != null)
			caches [type] = components;
		return components;
	}
	
	private T[] DynamicGets<T> (Component root, bool throwException) where T : Component
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

	private T[] DynamicGetsByScope<T> (Component root) where T : Component
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

	private T[] FindsInScope<T> (Component root, Scopes scope) where T : Component
	{
		if (root == null)
			throw CreateUnityException ("Must set 'from' before find");
		return FindsInScope<T> (root.gameObject, scope);
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
	public bool Require<T> () where T : Component
	{
		if (IsDefaultState) {
			var component = DynamicGet<T> (from, exceptionWhenNotFound);
			return component != null;
		}

		try {
			var component = DynamicGet<T> (from, exceptionWhenNotFound);
			return component != null;
		} catch (MissingComponentException ignore) {
			throw JumpHookedException (string.Format("Require {0}", typeof(T)));
		}
	}
	#pragma warning restore 168

	public bool Requires (params System.Type[] types)
	{ 
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

		if (0 < notFound.Count) {
			if (exceptionWhenNotFound) {
				var strTypes = notFound
					.Select (com => com.ToString())
					.ToArray();
				var join = string.Join (" / ", strTypes);

				if (IsDefaultState)
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

	#endregion
}
