using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Web;

namespace N2.Web
{
	/// <summary>
	/// A lightweight and somewhat forgiving URI helper class.
	/// </summary>
	public class Url
	{
		/// <summary>Ampersand string.</summary>
		public const string Amp = "&";

		/// <summary>The token used for resolving the management url.</summary>
		public const string ManagementUrlToken = "{ManagementUrl}";
		public const string ThemesUrlToken = "{ThemesUrl}";
        public const string SelectedQueryKeyToken = "{SelectedQueryKey}";

		static readonly string[] querySplitter = new[] {"&amp;", Amp};
		static readonly char[] slashes = new char[] { '/' };
		static readonly char[] dotsAndSlashes = new char[] { '.', '/' };

	    private static readonly HashSet<string> contentParameters = new HashSet<string>
		{
			PathData.ItemQueryKey,
			PathData.PageQueryKey,
			"action",
			"arguments"
		};

        static Dictionary<string, string> replacements = new Dictionary<string, string> { { ManagementUrlToken, "~/N2" }, { ThemesUrlToken, "~/App_Themes/" }, { SelectedQueryKeyToken, "selected" } };

	    public Url(Url other)
		{
			Scheme = other.Scheme;
			Authority = other.Authority;
			Path = other.Path;
			Query = other.Query;
			Fragment = other.Fragment;
		}

		public Url(string scheme, string authority, string path, string query, string fragment)
		{
			this.Scheme = scheme;
			this.Authority = authority;
			this.Path = path;
			this.Query = query;
			this.Fragment = fragment;
		}

		public Url(string scheme, string authority, string rawUrl)
		{
			int queryIndex = QueryIndex(rawUrl);
			int hashIndex = rawUrl.IndexOf('#', queryIndex > 0 ? queryIndex : 0);
			LoadFragment(rawUrl, hashIndex);
			LoadQuery(rawUrl, queryIndex, hashIndex);
			LoadSiteRelativeUrl(rawUrl, queryIndex, hashIndex);
			this.Scheme = scheme;
			this.Authority = authority;
		}

		public Url(string url)
		{
			if (url == null)
			{
				ClearUrl();
			}
			else
			{
				int queryIndex = QueryIndex(url);
				int hashIndex = url.IndexOf('#', queryIndex > 0 ? queryIndex : 0);
				int authorityIndex = url.IndexOf(Uri.SchemeDelimiter); // jamestharpe
				if (queryIndex >= 0 && authorityIndex > queryIndex)
					authorityIndex = -1;

				LoadFragment(url, hashIndex);
				LoadQuery(url, queryIndex, hashIndex);
				if (authorityIndex >= 0)
					LoadBasedUrl(url, queryIndex, hashIndex, authorityIndex);
				else
					LoadSiteRelativeUrl(url, queryIndex, hashIndex);
			}
		}

		void ClearUrl()
		{
			Scheme = null;
			Authority = null;
			Path = "";
			Query = null;
			Fragment = null;
		}

#if SAFE_URL_HANDLING
        void EnsureTrailingSlashOnPath()
		{
			// Addition by James Tharpe w/ Rollins, Inc.
			// --------------------------------------------------------------------------------
			// If current.Extension is blank, include a trailing slash so that URLs remain
			// consistent. Keeping URLs consistent is important for SEO reasons (specifically,
			// to avoid the appearance of duplicate content. See discussion at
			// http://n2cms.codeplex.com/discussions/277160.

			if (Extension == null && !path.EndsWith("/")) //TODO: Add a forceTralingSlash option?
				path += "/";
		}
#endif

		void LoadSiteRelativeUrl(string url, int queryIndex, int hashIndex)
		{
			Scheme = null;
			Authority = null;
			if (url.StartsWith("~"))
				url = ToAbsolute(url);

			if (queryIndex >= 0)
				Path = url.Substring(0, queryIndex);
			else if (hashIndex >= 0)
				Path = url.Substring(0, hashIndex);
			else if (url.Length > 0)
				Path = url;
			else
				Path = "";

#if SAFE_URL_HANDLING
            // alternatively it's possible to set extension="/"
			EnsureTrailingSlashOnPath(); // jamestharpe
#endif
		}

		void LoadBasedUrl(string url, int queryIndex, int hashIndex, int authorityIndex)
		{
			Scheme = url.Substring(0, authorityIndex); // e.g. "http://"
			int slashIndex = url.IndexOf('/', authorityIndex + 3);
			if (slashIndex > 0) // http://site.com/ or http://site.com/foo or http://site.com/foo/bar
			{
				Authority = url.Substring(authorityIndex + 3, slashIndex - authorityIndex - 3); // site.com
				if (queryIndex >= slashIndex)
					Path = url.Substring(slashIndex, queryIndex - slashIndex); // http://site.com/foo/bar?q=v -> /foo/bar
				else if (hashIndex >= 0)
					Path = url.Substring(slashIndex, hashIndex - slashIndex); // http://site.com/foo/bar#hash -> /foo/bar
				else
					Path = url.Substring(slashIndex); // http://site.com/foo/bar -> /foo/bar

#if SAFE_URL_HANDLING
				EnsureTrailingSlashOnPath(); // jamestharpe
#endif
			}
			else
			{
				// is this case tolerated?
				Authority = url.Substring(authorityIndex + 3);
				Path = "/";
			}
		}

		void LoadQuery(string url, int queryIndex, int hashIndex)
		{
			if (hashIndex >= 0 && queryIndex >= 0)
				Query = EmptyToNull(url.Substring(queryIndex + 1, hashIndex - queryIndex - 1));
			else if (queryIndex >= 0)
				Query = EmptyToNull(url.Substring(queryIndex + 1));
			else
				Query = null;
		}

		void LoadFragment(string url, int hashIndex)
		{
		    Fragment = hashIndex >= 0 ? EmptyToNull(url.Substring(hashIndex + 1)) : null;
		}

	    private static string EmptyToNull(string text)
		{
		    return string.IsNullOrEmpty(text) ? null : text;
		}

	    public Url HostUrl
		{
			get { return new Url(Scheme, Authority, string.Empty, null, null);}
		}

		public Url LocalUrl
		{
			get { return new Url(null, null, Path, Query, Fragment); }
		}

	    /// <summary>E.g. http</summary>
	    public string Scheme { get; private set; }

	    /// <summary>The domain name information.</summary>
        public string Domain
        {
            get { return Authority != null ? Authority.Split(':')[0] : null; }
        }

        /// <summary>The port information.</summary>
        public int Port
        {
            get { return Authority != null ? int.Parse(Authority.Split(':').Skip(1).FirstOrDefault() ?? "80") : 80; }
        }

	    /// <summary>The domain name and port information.</summary>
	    public string Authority { get; private set; }

	    /// <summary>The path after domain name and before query string, e.g. /path/to/a/page.aspx.</summary>
	    public string Path { get; private set; }

	    public string[] Segments
        {
            get
            {
                if (string.IsNullOrEmpty(Path) || Path == "/")
                    return new string[0];
                return Path.Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries);
            }
        }

		public string ApplicationRelativePath
		{
			get
			{
				string appPath = ApplicationPath;
				if (appPath.Equals("/"))
					return "~" + Path;
				return Path.StartsWith(appPath, StringComparison.InvariantCultureIgnoreCase) ? Path.Substring(appPath.Length) : Path;
			}
		}

	    /// <summary>The query string, e.g. key=value.</summary>
	    public string Query { get; private set; }

	    public string Extension
		{
			get
			{
				int index = Path.LastIndexOfAny(dotsAndSlashes);

				if (index < 0)
					return null;
				return Path[index] == '/' ? null : Path.Substring(index);
			}
		}

		public string PathWithoutExtension
		{
			get { return RemoveAnyExtension(Path); }
		}

		/// <summary>The combination of the path and the query string, e.g. /path.aspx?key=value.</summary>
		public string PathAndQuery
		{
			get { return string.IsNullOrEmpty(Query) ? Path : Path + "?" + Query; }
		}

	    /// <summary>The bookmark.</summary>
	    public string Fragment { get; private set; }

	    /// <summary>Tells whether the url contains authority information.</summary>
		public bool IsAbsolute
		{
			get { return Authority != null; }
		}

		public override string ToString()
		{
			string url;
			if (Authority != null)
				url = Scheme + "://" + Authority + Path;
			else
				url = Path;
			if (Query != null)
				url += "?" + Query;
			if (Fragment != null)
				url += "#" + Fragment;
			return url;
		}

		public static implicit operator string(Url u)
		{
			if (u == null)
				return null;
			return u.ToString();
		}

		public static implicit operator Url(string url)
		{
			return Parse(url);
		}

		public static explicit operator Uri(Url u)
		{
			if (u == null)
				return null;
			return new Uri(u.ToString(), UriKind.RelativeOrAbsolute);
		}

		public static implicit operator Url(Uri uri)
		{
			return Parse(uri.ToString());
		}

		/// <summary>Retrieves the path part of an url, e.g. /path/to/page.aspx.</summary>
		public static string PathPart(string url)
		{
			url = RemoveHash(url);

			int queryIndex = QueryIndex(url);
			if (queryIndex >= 0)
				url = url.Substring(0, queryIndex);

			return url;
		}

		/// <summary><![CDATA[Retrieves the query part of an url, e.g. page=12&value=something.]]></summary>
		public static string QueryPart(string url)
		{
			url = RemoveHash(url);

			int queryIndex = QueryIndex(url);
			if (queryIndex >= 0)
				return url.Substring(queryIndex + 1);
			return string.Empty;
		}

		static int QueryIndex(string url)
		{
			return url.IndexOf('?');
		}

	    /// <summary>The extension used for url's to content items.</summary>
	    public static string DefaultExtension { get; set; }

	    /// <summary>The default document to use when removing default document from paths.</summary>
	    public static string DefaultDocument { get; set; }

	    /// <summary>Removes the hash (#...) from an url.</summary>
		/// <param name="url">An url that might hav a hash in it.</param>
		/// <returns>An url without the hash part.</returns>
		public static string RemoveHash(string url)
		{
			if (url == null) return null;

			int hashIndex = url.IndexOf('#');
			if (hashIndex >= 0)
				url = url.Substring(0, hashIndex);
			return url;
		}

		/// <summary>Converts a string URI to an Url class. The method will make any app-relative managementUrls absolute.</summary>
		/// <param name="url">The URI to parse.</param>
		/// <returns>An Url object or null if the input was null.</returns>
		public static Url Parse(string url)
		{
			if (url == null) return null;

			if (url.StartsWith("~"))
				url = ToAbsolute(url);

			return new Url(url);
		}

		/// <summary>Converts a string URI to an Url class. The method will make any app-relative managementUrls absolute and resolve tokens within the url string.</summary>
		/// <param name="url">The URI to parse.</param>
		/// <returns>An Url object or null if the input was null.</returns>
		public static Url ParseTokenized(string url)
		{
			return Url.Parse(ResolveTokens(url));
		}

		public string GetQuery(string key)
		{
			IDictionary<string, string> queries = GetQueries();
			return queries.ContainsKey(key) ? queries[key] : null;
		}

		public string this[string queryKey]
		{
			get { return GetQuery(queryKey); }
		}

		public IDictionary<string, string> GetQueries()
		{
			return ParseQueryString(Query);
		}

		public Url AppendQuery(string key, string value)
		{
			return AppendQuery(key, value, true);
		}

		public Url AppendQuery(string key, string value, bool unlessNull)
		{
			if (unlessNull && value == null)
				return this;
				 
			return AppendQuery(key + "=" + HttpUtility.UrlEncode(value));
		}

		public Url AppendQuery(string key, int value)
		{
			return AppendQuery(key + "=" + value);
		}

		public Url AppendQuery(string key, bool value)
		{
			return AppendQuery(key + (value ? "=true" : "=false"));
		}

		public Url AppendQuery(string key, object value)
		{
			if (value == null)
				return this;
			
			return AppendQuery(key + "=" + value);
		}

		public Url AppendQuery(string keyValue)
		{
			var clone = new Url(this);
			if (string.IsNullOrEmpty(Query))
				clone.Query = keyValue;
			else if (!string.IsNullOrEmpty(keyValue))
				clone.Query += Amp + keyValue;
			return clone;
		}

		public Url SetQueryParameter(string key, int value)
		{
			return SetQueryParameter(key, value.ToString());
		}

		public Url SetQueryParameter(string key, string value)
		{
			return SetQueryParameter(key, value, false);
		}

		public Url RemoveQuery(string key)
		{
			return SetQueryParameter(key, null, true);
		}

		public Url SetQueryParameter(string key, string value, bool removeNullValue)
		{
			if (removeNullValue && value == null && Query == null)
				return this;
			if (Query == null)
				return AppendQuery(key, value);

			var clone = new Url(this);
			string[] queries = Query.Split(querySplitter, StringSplitOptions.RemoveEmptyEntries);
			for (int i = 0; i < queries.Length; i++)
			{
				if (queries[i].StartsWith(key + "=", StringComparison.InvariantCultureIgnoreCase))
				{
					if (value != null)
					{
						queries[i] = key + "=" + HttpUtility.UrlEncode(value);
						clone.Query = string.Join(Amp, queries);
						return clone;
					}
					
					if (queries.Length == 1)
						clone.Query = null;
					else if (Query.Length == 2)
						clone.Query = queries[i == 0 ? 1 : 0];
					else if (i == 0)
						clone.Query = string.Join(Amp, queries, 1, queries.Length - 1);
					else if (i == queries.Length - 1)
						clone.Query = string.Join(Amp, queries, 0, queries.Length - 1);
					else
						clone.Query = string.Join(Amp, queries, 0, i) + Amp + string.Join(Amp, queries, i + 1, queries.Length - i - 1);
					return clone;
				}
			}
			return AppendQuery(key, value);
		}

		public Url SetQueryParameter(string keyValue)
		{
			if (Query == null)
				return AppendQuery(keyValue);

			int eqIndex = keyValue.IndexOf('=');
			if (eqIndex >= 0)
				return SetQueryParameter(keyValue.Substring(0, eqIndex), keyValue.Substring(eqIndex + 1));
			else
				return SetQueryParameter(keyValue, string.Empty);
		}

		public Url SetScheme(string scheme)
		{
			return new Url(scheme, Authority, Path, Query, Fragment);
		}

		public Url SetAuthority(string authority)
		{
			return new Url(Scheme ?? "http", authority, Path, Query, Fragment);
		}

		public Url SetPath(string path)
		{
			if (path.StartsWith("~"))
				path = ToAbsolute(path);
			int queryIndex = QueryIndex(path);
			return new Url(Scheme, Authority, queryIndex < 0 ? path : path.Substring(0, queryIndex), Query, Fragment);
		}

		public Url SetQuery(string query)
		{
			return new Url(Scheme, Authority, Path, query, Fragment);
		}

		public Url SetExtension(string extension)
		{
			return new Url(Scheme, Authority, PathWithoutExtension.TrimEnd('/') + extension, Query, Fragment);
		}



		/// <summary>Returns an url with the specified fragment</summary>
		/// <param name="fragment">The fragment to use in the Url.</param>
		/// <returns>An url with the given fragment.</returns>
		public Url SetFragment(string fragment)
		{
			if (fragment == null)
				return this; 
			
			return new Url(Scheme, Authority, Path, Query, fragment.TrimStart('#'));
		}

		public Url AppendSegment(string segment, string extension)
		{
			string newPath;
			if (string.IsNullOrEmpty(Path) || Path == "/")
				newPath = "/" + segment + extension;
			else if (!string.IsNullOrEmpty(extension))
			{
				int extensionIndex = Path.LastIndexOf(extension);
				if (extensionIndex >= 0)
					newPath = Path.Insert(extensionIndex, "/" + segment);
				else if (Path.EndsWith("/"))
					newPath = Path + segment + extension;
				else
					newPath = Path + "/" + segment + extension;
			}
			else if (Path.EndsWith("/"))
			{
				if (segment.StartsWith("/"))
					newPath = Path + segment.TrimStart('/');
				else
					newPath = Path + segment;
			}
			else if (segment.StartsWith("/"))
				newPath = Path + segment;
			else
				newPath = Path + "/" + segment;

			return new Url(Scheme, Authority, newPath, Query, Fragment);
		}

		public Url Append(Url url)
		{
			return AppendSegment(url.PathWithoutExtension, url.Extension);
		}

		public Url AppendSegment(string segment)
		{
			if (string.IsNullOrEmpty(Path) || Path == "/")
				return AppendSegment(segment, DefaultExtension);

			if (string.IsNullOrEmpty(segment) || segment == "/")
				return this;

			return AppendSegment(segment, Extension);
		}

		public Url AppendSegment(string segment, bool useDefaultExtension)
		{
			return AppendSegment(segment, useDefaultExtension ? DefaultExtension : Extension);
		}

		public Url PrependSegment(string segment, string extension)
		{
			if (string.IsNullOrEmpty(segment))
				return this;

			string newPath;
			if (string.IsNullOrEmpty(Path) || Path == "/")
				newPath = "/" + segment.TrimStart('/') + extension;
			else if (extension != Extension)
			{
				newPath = "/" + segment + PathWithoutExtension + extension;
			}
			else
			{
				newPath = "/" + segment.Trim('/') + "/" + Path.TrimStart('/');
			}

			return new Url(Scheme, Authority, newPath, Query, Fragment);
		}

		public Url PrependSegment(string segment)
		{
			if (string.IsNullOrEmpty(Path) || Path == "/")
				return PrependSegment(segment, DefaultExtension);
			
			return PrependSegment(segment, Extension);
		}

		public Url UpdateQuery(NameValueCollection queryString)
		{
			Url u = new Url(this);
			foreach (string key in queryString.AllKeys)
				u = u.SetQueryParameter(key, queryString[key]);
			return u;
		}

		public Url UpdateQuery(IDictionary<string,string> queryString)
		{
			Url u = new Url(this);
			foreach (KeyValuePair<string,string> pair in queryString)
				u = u.SetQueryParameter(pair.Key, pair.Value);
			return u;
		}

		public Url UpdateQuery(IDictionary<string,object> queryString)
		{
			Url u = new Url(this);
			foreach (KeyValuePair<string,object> pair in queryString)
				if(pair.Value != null)
					u = u.SetQueryParameter(pair.Key, pair.Value.ToString());
			return u;
		}

		/// <summary>Returns the url without the file extension (if any).</summary>
		/// <returns>An url with it's extension removed.</returns>
		public Url RemoveExtension()
		{
			return new Url(Scheme, Authority, PathWithoutExtension, Query, Fragment);
		}

		/// <summary>Returns the url without the file extension (if any).</summary>
		/// <param name="validExtensions">Extensions that may be removed.</param>
		/// <returns>An url with it's extension removed.</returns>
		public Url RemoveExtension(params string[] validExtensions)
		{
			var pathExtension = Array.Find(validExtensions, x => string.IsNullOrEmpty(x) == false && Path.EndsWith(x, StringComparison.InvariantCultureIgnoreCase));
			if (pathExtension == null)
				return this;
			return new Url(Scheme, Authority, Path.Substring(0, Path.Length - pathExtension.Length), Query, Fragment);
		}

		/// <summary>Removes the given default document from the end of the url, if there.</summary>
		/// <param name="defualtDocument">The default document to remove, e.g. "Default.aspx"</param>
		/// <returns>An url without ending default document, or the same url if no ending default document.</returns>
		public Url RemoveDefaultDocument(string defualtDocument)
		{
			if (!Path.EndsWith("/" + defualtDocument, StringComparison.InvariantCultureIgnoreCase))
				return this;

			return new Url(Scheme, Authority, RemoveLastSegment(Path), Query, Fragment);
		}

	    /// <summary>Converts a possibly relative to an absolute url.</summary>
		/// <param name="path">The url to convert.</param>
		/// <returns>The absolute url.</returns>
		public static string ToAbsolute(string path)
		{
			return ToAbsolute(ApplicationPath, path);
		}

		/// <summary>Converts a possibly relative to an absolute url.</summary>
		/// <param name="applicationPath">The application path to be absolute about.</param>
		/// <param name="path">The url to convert.</param>
		/// <returns>The absolute url.</returns>
		public static string ToAbsolute(string applicationPath, string path)
		{
			if (!string.IsNullOrEmpty(path) && path[0] == '~' && path.Length > 1)
				return applicationPath + path.Substring(2);
			else if (path == "~")
				return applicationPath;
			return path;
		}

		/// <summary>Converts a virtual path to a relative path, e.g. /myapp/path/to/a/page.aspx -> ~/path/to/a/page.aspx</summary>
		/// <param name="path">The virtual path.</param>
		/// <returns>A relative path</returns>
		public static string ToRelative(string path)
		{
			return ToRelative(ApplicationPath, path);
		}

		/// <summary>Converts a virtual path to a relative path, e.g. /myapp/path/to/a/page.aspx -> ~/path/to/a/page.aspx</summary>
		/// <param name="path">The virtual path.</param>
		/// <returns>A relative path</returns>
		public static string ToRelative(string applicationPath, string path)
		{
			if (!string.IsNullOrEmpty(path) && path.StartsWith(applicationPath, StringComparison.OrdinalIgnoreCase))
				return "~/" + path.Substring(applicationPath.Length);
			return path;
		}

		static string applicationPath;
		/// <summary>Gets the root path of the web application. e.g. "/" if the application doesn't run in a virtual directory.</summary>
		public static string ApplicationPath
		{
			get
			{
				if (applicationPath == null)
				{
					if(HttpContext.Current == null)
						return "/";

					try
					{
						applicationPath = VirtualPathUtility.ToAbsolute("~/");
					}
					catch
					{
						return "/";
					}
				}
				return applicationPath;
			}
			set { applicationPath = value; }
		}

		private static string fallbackServerUrl;

	    static Url()
	    {
	        DefaultDocument = "Default.aspx";
	        DefaultExtension = "";
	    }

	    /// <summary>The address to the server where the site is running.</summary>
		public static string ServerUrl
		{
			get
			{
				// we need get the server url of current request, it can't be cached in multiple-sites environment 
				var webContext = Context.Current.RequestContext;
				if (webContext == null)
					return null;
				if(webContext.IsWeb)
				{
					if(fallbackServerUrl == null)
						fallbackServerUrl = webContext.Url.HostUrl;
					return webContext.Url.HostUrl;
				}
				return fallbackServerUrl; // fallback for ThreadContext
			}
		}

		/// <summary>Removes the file extension from a path.</summary>
		/// <param name="path">The server relative path.</param>
		/// <returns>The path without the file extension or the same path if no extension was found.</returns>
		public static string RemoveAnyExtension(string path)
		{
			int index = path.LastIndexOfAny(dotsAndSlashes);

			if (index < 0)
				return path;
			if (path[index] == '/')
				return path;

			return path.Substring(0, index);
		}

        /// <summary>Gets the file extension from the path (if any).</summary>
        /// <param name="path">The path to find the extension of.</param>
        /// <returns>An extension including . or null if no extnesion was found</returns>
        public static string GetExtension(string path)
        {
            int index = path.LastIndexOfAny(dotsAndSlashes);

            if (index < 0)
                return null;
            if (path[index] == '/')
                return null;

            return path.Substring(index);
        }

		/// <summary>Removes the last part from the url segments.</summary>
		/// <returns></returns>
		public Url RemoveTrailingSegment(bool maintainExtension)
		{
			if(string.IsNullOrEmpty(Path) || Path == "/")
				return this;

			string newPath = "/";

			int lastSlashIndex = Path.LastIndexOf('/');
			if (lastSlashIndex == Path.Length - 1)
				lastSlashIndex = Path.TrimEnd(slashes).LastIndexOf('/');
			if (lastSlashIndex > 0)
				newPath = Path.Substring(0, lastSlashIndex) + (maintainExtension ? Extension : "");

			return new Url(Scheme, Authority, newPath, Query, Fragment);
		}

		/// <summary>Removes the last part from the url segments.</summary>
		public Url RemoveTrailingSegment()
		{
			return RemoveTrailingSegment(true);
		}

		/// <summary>Removes the segment at the specified location.</summary>
		/// <param name="index">The segment index to remove</param>
		/// <returns>An url without the specified segment.</returns>
		public Url RemoveSegment(int index)
		{
			if (string.IsNullOrEmpty(Path) || Path == "/" || index < 0)
				return this;

			if(index == 0)
			{
				int slashIndex = Path.IndexOf('/', 1);
				if(slashIndex < 0)
					return new Url(Scheme, Authority, "/", Query, Fragment);
				return new Url(Scheme, Authority, Path.Substring(slashIndex), Query, Fragment);
			}

			string[] segments = PathWithoutExtension.Split(slashes, StringSplitOptions.RemoveEmptyEntries);
			if(index >= segments.Length)
				return this;

			if (index == segments.Length - 1)
				return RemoveTrailingSegment();

			string newPath = "/" + string.Join("/", segments, 0, index) + "/" + string.Join("/", segments, index + 1, segments.Length - index - 1) + Extension;
			return new Url(Scheme, Authority, newPath, Query, Fragment);
		}

		/// <summary>Removes the last segment from a path, e.g. "/hello/world" -> "/hello/"</summary>
		/// <param name="path"></param>
		/// <returns></returns>
		public static string RemoveLastSegment(string path)
		{
			if (string.IsNullOrEmpty(path))
				return null;
			
			int slashIndex = GetLastSignificatSlash(path);
			return path.Substring(0, slashIndex + 1);
		}

		/// <summary>Gets the last segment of a path, e.g. "/hello/world/" -> "world"</summary>
		/// <param name="path">The path whose name to get.</param>
		/// <returns>The name of the path or empty.</returns>
		public static string GetName(string path)
		{
			if (string.IsNullOrEmpty(path))
				return null;

			int slashIndex = GetLastSignificatSlash(path);
			int lastSlashIndex = path.LastIndexOf('/');
			if (lastSlashIndex == slashIndex)
				return path.Substring(slashIndex + 1);

			return path.Substring(slashIndex + 1, lastSlashIndex - slashIndex - 1);
		}

		private static int GetLastSignificatSlash(string path)
		{
			int i = path.Length - 1;
			for (; i >= 0; i--)
			{
				if (path[i] != '/')
					break;
			}
			for (; i >= 0; i--)
			{
				if (path[i] == '/')
					break;
			}
			return i;
		}

		public string Encode()
		{
			return ToString().Replace(Amp, "&amp;");
		}

		/// <summary>Mimics the behavior of VirtualPathUtility.Combine with less restrictions and minimal dependencies.</summary>
		/// <param name="url1">First part</param>
		/// <param name="url2">Last part</param>
		/// <returns>The combined URL.</returns>
		public static string Combine(string url1, string url2)
		{
			if (string.IsNullOrEmpty(url2))
				return ToAbsolute(url1);
			if(string.IsNullOrEmpty(url1))
				return ToAbsolute(url2);

			if (url2.StartsWith("/"))
				return url2;
			if (url2.StartsWith("~"))
				return ToAbsolute(url2);
			if (url2.StartsWith("{"))
				return url2;
			if (url2.IndexOf(':') >= 0)
				return url2;

			Url first = url1;
			Url second = url2;

			return first.AppendSegment(second.Path, "").AppendQuery(second.Query).SetFragment(second.Fragment);
		}

		/// <summary>Converts a text query string to a dictionary.</summary>
		/// <param name="query">The query string</param>
		/// <returns>A dictionary of the query parts.</returns>
		public static IDictionary<string, string> ParseQueryString(string query)
		{
            var dictionary = new Dictionary<string, string>(StringComparer.InvariantCultureIgnoreCase);
			if (query == null)
				return dictionary;

			string[] queries = query.Split(querySplitter, StringSplitOptions.RemoveEmptyEntries);
			for (int i = 0; i < queries.Length; i++)
			{
				string q = queries[i];
				int eqIndex = q.IndexOf("=");
				if (eqIndex >= 0)
					dictionary[q.Substring(0, eqIndex)] = q.Substring(eqIndex + 1);
			}
			return dictionary;
		}

		/// <summary>Changes the application base of an url.</summary>
		/// <param name="currentPath">Replaces an absolute url in one app, to the absolute path of another app.</param>
		/// <param name="fromAppPath">The origin application path.</param>
		/// <param name="toAppPath">The destination application path.</param>
		/// <returns>A rebased url.</returns>
		public static string Rebase(string currentPath, string fromAppPath, string toAppPath)
		{
			if (currentPath == null || fromAppPath == null || !currentPath.StartsWith(fromAppPath))
				return currentPath;

			return toAppPath + currentPath.Substring(fromAppPath.Length);
		}

		/// <summary>Gets a replacement used by <see cref="ResolveTokens"/>.</summary>
		/// <param name="token"></param>
		/// <returns></returns>
		public static string GetToken(string token)
		{
			string value = null;
			replacements.TryGetValue(token, out value);
			return value;
		}

		/// <summary>Adds a replacement used by <see cref="ResolveTokens"/>.</summary>
		/// <param name="token">They token to replace.</param>
		/// <param name="value">The value to replace the token with.</param>
		public static void SetToken(string token, string value)
		{
			if(token == null) throw new ArgumentNullException("key");

			if (value != null)
				replacements[token] = value;
			else if (replacements.ContainsKey(token))
				replacements.Remove(token);
		}

		/// <summary>Formats an url using replacement tokens.</summary>
		/// <param name="urlFormat">The url format to resolve, e.g. {ManagementUrl}/Resources/icons/add.png</param>
		/// <returns>An an absolut path with all tokens replaced by their corresponding value.</returns>
		public static string ResolveTokens(string urlFormat)
		{
			if (string.IsNullOrEmpty(urlFormat))
				return urlFormat;

            //TODO: Use a nicer method for replacing tokens. 

            const int maxLevels = 10; // Does work if token values contain other tokens, replacement levels hard limited.
            for (int level = 0; level < maxLevels; ++level)
            {
                bool changed = false;
                foreach (var kvp in replacements)
                {
                    string replaced = urlFormat.Replace(kvp.Key, kvp.Value);
                    if (replaced != urlFormat)
                        changed = true;
                    urlFormat = replaced;
                }

                if (!changed)
                    break; // all resolved
            }

            return ToAbsolute(urlFormat);
		}

		/// <summary>Formsats this url using replacement tokens.</summary>
		/// <returns>An url without replacement tokens.</returns>
		public Url ResolveTokens()
		{
			return new Url(ResolveTokens(ToString()));
		}

	    /// <summary>
	    /// Removes anything from the url that doesn't affect the content to be returned.
	    /// </summary>
	    /// <returns>Normalized url.</returns>
	    public Url GetNormalizedContentUrl()
		{
			var clonedUrl = new Url(this);

			// Normalize the url remove parameters we don't care about
			foreach (var queryParameter in GetQueries())
			{
				if (!contentParameters.Contains(queryParameter.Key.ToLowerInvariant()))
					clonedUrl = RemoveQuery(queryParameter.Key);
			}

			return clonedUrl;
		}
	}
}
