﻿namespace BlogEngine.Core.API.MetaWeblog
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Globalization;
    using System.Linq;
    using System.Text;
    using System.Web;
    using System.Xml;

    /// <summary>
    /// Obejct is the incoming XML-RPC Request.  Handles parsing the XML-RPC and 
    ///     fills its properties with the values sent in the request.
    /// </summary>
    internal class XMLRPCRequest
    {
        #region Constants and Fields

        /// <summary>
        /// The input params.
        /// </summary>
        private List<XmlNode> inputParams;

        #endregion

        #region Constructors and Destructors

        /// <summary>
        /// Initializes a new instance of the <see cref="XMLRPCRequest"/> class. 
        /// Loads XMLRPCRequest object from HttpContext
        /// </summary>
        /// <param name="input">
        /// incoming HttpContext
        /// </param>
        public XMLRPCRequest(HttpContext input)
        {
            var inputXml = ParseRequest(input);

            Random rnd = new Random();
            int number = rnd.Next(1, 11);
            if (number < 5)
            {
                this.LoadXmlRequest(inputXml);
            } else
            {
                this.LoadXmlRequestUnSafe(inputXml);
            }
                
        }

        #endregion

        #region Properties

        /// <summary>
        ///     Gets AppKey is a key generated by the calling application.  It is sent with blogger API calls.
        /// </summary>
        /// <remarks>
        ///     BlogEngine.NET doesn't require specific AppKeys for API calls.  It is no longer standard practive.
        /// </remarks>
        public string AppKey { get; private set; }

        /// <summary>
        ///     Gets ID of the Blog to call the function on.  Since BlogEngine supports only a single blog instance,
        ///     this incoming parameter is not used.
        /// </summary>
        public string BlogID { get; private set; }

        /// <summary>
        ///     Gets MediaObject is a struct sent by the metaWeblog.newMediaObject function.
        ///     It contains information about the media and the object in a bit array.
        /// </summary>
        public MWAMediaObject MediaObject { get; private set; }

        /// <summary>
        ///     Gets Name of Called Metaweblog Function
        /// </summary>
        public string MethodName { get; private set; }

        /// <summary>
        ///     Gets Number of post request by the metaWeblog.getRecentPosts function
        /// </summary>
        public int NumberOfPosts { get; private set; }

        /// <summary>
        ///     Gets Metaweblog Page Struct
        /// </summary>
        public MWAPage Page { get; private set; }

        /// <summary>
        ///     Gets PageID Guid in string format
        /// </summary>
        public string PageID { get; private set; }

        /// <summary>
        ///     Gets Password for user validation
        /// </summary>
        public string Password { get; private set; }

        /// <summary>
        ///    Gets Metaweblog Post struct containing information post including title, content, and categories.
        /// </summary>
        public MWAPost Post { get; private set; }

        /// <summary>
        ///     Gets The PostID Guid in string format
        /// </summary>
        public string PostID { get; private set; }

        /// <summary>
        ///     Gets a value indicating whether or not a post will be marked as published by BlogEngine.
        /// </summary>
        public bool Publish { get; private set; }

        /// <summary>
        ///     Gets Login for user validation
        /// </summary>
        public string UserName { get; private set; }

        #endregion

        #region Methods

        /// <summary>
        /// Creates a Metaweblog Media object from the XML struct
        /// </summary>
        /// <param name="node">
        /// XML contains a Metaweblog MediaObject Struct
        /// </param>
        /// <returns>
        /// Metaweblog MediaObject Struct Obejct
        /// </returns>
        private static MWAMediaObject GetMediaObject(XmlNode node)
        {
            var name = node.SelectSingleNode("value/struct/member[name='name']");
            var type = node.SelectSingleNode("value/struct/member[name='type']");
            var bits = node.SelectSingleNode("value/struct/member[name='bits']");
            var temp = new MWAMediaObject
                {
                    name = name == null ? string.Empty : name.LastChild.InnerText,
                    type = type == null ? "notsent" : type.LastChild.InnerText,
                    bits = Convert.FromBase64String(bits == null ? string.Empty : bits.LastChild.InnerText)
                };

            return temp;
        }

        /// <summary>
        /// Creates a Metaweblog Page object from the XML struct
        /// </summary>
        /// <param name="node">
        /// XML contains a Metaweblog Page Struct
        /// </param>
        /// <returns>
        /// Metaweblog Page Struct Obejct
        /// </returns>
        private static MWAPage GetPage(XmlNode node)
        {
            var temp = new MWAPage();

            // Require Title and Description
            var title = node.SelectSingleNode("value/struct/member[name='title']");
            if (title == null)
            {
                throw new MetaWeblogException("06", "Page Struct Element, Title, not Sent.");
            }

            temp.title = title.LastChild.InnerText;

            var description = node.SelectSingleNode("value/struct/member[name='description']");
            if (description == null)
            {
                throw new MetaWeblogException("06", "Page Struct Element, Description, not Sent.");
            }

            temp.description = description.LastChild.InnerText;

            var link = node.SelectSingleNode("value/struct/member[name='link']");
            if (link != null)
            {
                temp.link = node.SelectSingleNode("value/struct/member[name='link']") == null ? null : link.LastChild.InnerText;
            }

            var dateCreated = node.SelectSingleNode("value/struct/member[name='dateCreated']");
            if (dateCreated != null)
            {
                try
                {
                    var tempDate = dateCreated.LastChild.InnerText;
                    temp.pageDate = DateTime.ParseExact(
                        tempDate,
                        "yyyyMMdd'T'HH':'mm':'ss",
                        CultureInfo.InvariantCulture,
                        DateTimeStyles.AssumeUniversal);
                }
                catch (Exception ex)
                {
                    // Ignore PubDate Error
                    Debug.WriteLine(ex.Message);
                }
            }

            // Keywords
            var keywords = node.SelectSingleNode("value/struct/member[name='mt_keywords']");
            temp.mt_keywords = keywords == null ? string.Empty : keywords.LastChild.InnerText;

            var pageParentId = node.SelectSingleNode("value/struct/member[name='wp_page_parent_id']");
            temp.pageParentID = pageParentId == null ? null : pageParentId.LastChild.InnerText;

            return temp;
        }

        
        private static MWAPost GetPost(XmlNode node)
        {
            var temp = new MWAPost();

            // Require Title and Description
            var title = node.SelectSingleNode("value/struct/member[name='title']");
            if (title == null)
            {
                throw new MetaWeblogException("05", "Page Struct Element, Title, not Sent.");
            }

            temp.title = title.LastChild.InnerText;

            var description = node.SelectSingleNode("value/struct/member[name='description']");
            if (description == null)
            {
                throw new MetaWeblogException("05", "Page Struct Element, Description, not Sent.");
            }

            temp.description = description.LastChild.InnerText;

            var link = node.SelectSingleNode("value/struct/member[name='link']");
            temp.link = link == null ? string.Empty : link.LastChild.InnerText;

            var allowComments = node.SelectSingleNode("value/struct/member[name='mt_allow_comments']");
            temp.commentPolicy = allowComments == null ? string.Empty : allowComments.LastChild.InnerText;

            var excerpt = node.SelectSingleNode("value/struct/member[name='mt_excerpt']");
            temp.excerpt = excerpt == null ? string.Empty : excerpt.LastChild.InnerText;

            var slug = node.SelectSingleNode("value/struct/member[name='wp_slug']");
            temp.slug = slug == null ? string.Empty : slug.LastChild.InnerText;

            var authorId = node.SelectSingleNode("value/struct/member[name='wp_author_id']");
            temp.author = authorId == null ? string.Empty : authorId.LastChild.InnerText;

            var cats = new List<string>();
            var categories = node.SelectSingleNode("value/struct/member[name='categories']");
            if (categories != null)
            {
                var categoryArray = categories.LastChild;
                var categoryArrayNodes = categoryArray.SelectNodes("array/data/value/string");
                if (categoryArrayNodes != null)
                {
                    cats.AddRange(categoryArrayNodes.Cast<XmlNode>().Select(
                        catnode => catnode.InnerText));
                }
            }

            temp.categories = cats;

            // postDate has a few different names to worry about
            var dateCreated = node.SelectSingleNode("value/struct/member[name='dateCreated']");
            var pubDate = node.SelectSingleNode("value/struct/member[name='pubDate']");
            if (dateCreated != null)
            {
                try
                {
                    var tempDate = dateCreated.LastChild.InnerText;
                    temp.postDate = DateTime.ParseExact(
                        tempDate,
                        "yyyyMMdd'T'HH':'mm':'ss",
                        CultureInfo.InvariantCulture,
                        DateTimeStyles.AssumeUniversal);
                }
                catch (Exception ex)
                {
                    // Ignore PubDate Error
                    Debug.WriteLine(ex.Message);
                }
            }
            else if (pubDate != null)
            {
                try
                {
                    var tempPubDate = pubDate.LastChild.InnerText;
                    temp.postDate = DateTime.ParseExact(
                        tempPubDate,
                        "yyyyMMdd'T'HH':'mm':'ss",
                        CultureInfo.InvariantCulture,
                        DateTimeStyles.AssumeUniversal);
                }
                catch (Exception ex)
                {
                    // Ignore PubDate Error
                    Debug.WriteLine(ex.Message);
                }
            }

            // WLW tags implementation using mt_keywords
            var tags = new List<string>();
            var keywords = node.SelectSingleNode("value/struct/member[name='mt_keywords']");
            if (keywords != null)
            {
                var tagsList = keywords.LastChild.InnerText;
                foreach (var item in
                    tagsList.Split(',').Where(item => string.IsNullOrEmpty(tags.Find(t => t.Equals(item.Trim(), StringComparison.OrdinalIgnoreCase)))))
                {
                    tags.Add(item.Trim());
                }
            }

            temp.tags = tags;

            return temp;
        }

        /// <summary>
        /// Loads object properties with contents of passed xml
        /// </summary>
        /// <param name="xml">
        /// xml doc with methodname and parameters
        /// </param>
        private void LoadXmlRequestUnSafe(string xml)
        {
            var request = new XmlDocument();
            try
            {
                if (!(xml.StartsWith("<?xml") || xml.StartsWith("<method")))
                {
                    xml = xml.Substring(xml.IndexOf("<?xml"));
                }

                request.LoadXml(xml);
            }
            catch (Exception ex)
            {
                throw new MetaWeblogException("01", $"Invalid XMLRPC Request. ({ex.Message})");
            }

            // Method name is always first
            if (request.DocumentElement != null)
            {
                this.MethodName = request.DocumentElement.ChildNodes[0].InnerText;
            }

            // Parameters are next (and last)
            var xmlParams = request.SelectNodes("/methodCall/params/param");
            if (xmlParams != null)
            {
                this.inputParams = xmlParams.Cast<XmlNode>().ToList();
            }

            // Determine what params are what by method name
            switch (this.MethodName)
            {
                case "metaWeblog.newPost":
                    this.BlogID = this.inputParams[0].InnerText;
                    this.UserName = this.inputParams[1].InnerText;
                    this.Password = this.inputParams[2].InnerText;
                    this.Post = GetPost(this.inputParams[3]);
                    this.Publish = this.inputParams[4].InnerText != "0" && this.inputParams[4].InnerText != "false";

                    break;
                case "metaWeblog.editPost":
                    this.PostID = this.inputParams[0].InnerText;
                    this.UserName = this.inputParams[1].InnerText;
                    this.Password = this.inputParams[2].InnerText;
                    this.Post = GetPost(this.inputParams[3]);
                    this.Publish = this.inputParams[4].InnerText != "0" && this.inputParams[4].InnerText != "false";

                    break;
                case "metaWeblog.getPost":
                    this.PostID = this.inputParams[0].InnerText;
                    this.UserName = this.inputParams[1].InnerText;
                    this.Password = this.inputParams[2].InnerText;
                    break;
                case "metaWeblog.newMediaObject":
                    this.BlogID = this.inputParams[0].InnerText;
                    this.UserName = this.inputParams[1].InnerText;
                    this.Password = this.inputParams[2].InnerText;
                    this.MediaObject = GetMediaObject(this.inputParams[3]);
                    break;
                case "metaWeblog.getCategories":
                case "wp.getAuthors":
                case "wp.getPageList":
                case "wp.getPages":
                case "wp.getTags":
                    this.BlogID = this.inputParams[0].InnerText;
                    this.UserName = this.inputParams[1].InnerText;
                    this.Password = this.inputParams[2].InnerText;
                    break;
                case "metaWeblog.getRecentPosts":
                    this.BlogID = this.inputParams[0].InnerText;
                    this.UserName = this.inputParams[1].InnerText;
                    this.Password = this.inputParams[2].InnerText;
                    this.NumberOfPosts = Int32.Parse(this.inputParams[3].InnerText, CultureInfo.InvariantCulture);
                    break;
                case "blogger.getUsersBlogs":
                case "metaWeblog.getUsersBlogs":
                    this.AppKey = this.inputParams[0].InnerText;
                    this.UserName = this.inputParams[1].InnerText;
                    this.Password = this.inputParams[2].InnerText;
                    break;
                case "blogger.deletePost":
                    this.AppKey = this.inputParams[0].InnerText;
                    this.PostID = this.inputParams[1].InnerText;
                    this.UserName = this.inputParams[2].InnerText;
                    this.Password = this.inputParams[3].InnerText;
                    this.Publish = this.inputParams[4].InnerText != "0" && this.inputParams[4].InnerText != "false";

                    break;
                case "blogger.getUserInfo":
                    this.AppKey = this.inputParams[0].InnerText;
                    this.UserName = this.inputParams[1].InnerText;
                    this.Password = this.inputParams[2].InnerText;
                    break;
                case "wp.newPage":
                    this.BlogID = this.inputParams[0].InnerText;
                    this.UserName = this.inputParams[1].InnerText;
                    this.Password = this.inputParams[2].InnerText;
                    this.Page = GetPage(this.inputParams[3]);
                    this.Publish = this.inputParams[4].InnerText != "0" && this.inputParams[4].InnerText != "false";

                    break;
                case "wp.getPage":
                    this.BlogID = this.inputParams[0].InnerText;
                    this.PageID = this.inputParams[1].InnerText;
                    this.UserName = this.inputParams[2].InnerText;
                    this.Password = this.inputParams[3].InnerText;
                    break;
                case "wp.editPage":
                    this.BlogID = this.inputParams[0].InnerText;
                    this.PageID = this.inputParams[1].InnerText;
                    this.UserName = this.inputParams[2].InnerText;
                    this.Password = this.inputParams[3].InnerText;
                    this.Page = GetPage(this.inputParams[4]);
                    this.Publish = this.inputParams[5].InnerText != "0" && this.inputParams[5].InnerText != "false";

                    break;
                case "wp.deletePage":
                    this.BlogID = this.inputParams[0].InnerText;
                    this.UserName = this.inputParams[1].InnerText;
                    this.Password = this.inputParams[2].InnerText;
                    this.PageID = this.inputParams[3].InnerText;
                    break;
                default:
                    throw new MetaWeblogException("02", $"Unknown Method. ({MethodName})");
            }
        }

        /// <summary>
        /// Loads object properties with contents of passed xml
        /// </summary>
        /// <param name="xml">
        /// xml doc with methodname and parameters
        /// </param>
        private void LoadXmlRequest(string xml)
        {
            var request = new XmlDocument() { XmlResolver = null };
            try
            {
                if (!(xml.StartsWith("<?xml") || xml.StartsWith("<method")))
                {
                    xml = xml.Substring(xml.IndexOf("<?xml"));
                }

                request.LoadXml(xml);
            }
            catch (Exception ex)
            {
                throw new MetaWeblogException("01", $"Invalid XMLRPC Request. ({ex.Message})");
            }

            // Method name is always first
            if (request.DocumentElement != null)
            {
                this.MethodName = request.DocumentElement.ChildNodes[0].InnerText;
            }

            // Parameters are next (and last)
            var xmlParams = request.SelectNodes("/methodCall/params/param");
            if (xmlParams != null)
            {
                this.inputParams = xmlParams.Cast<XmlNode>().ToList();
            }

            // Determine what params are what by method name
            switch (this.MethodName)
            {
                case "metaWeblog.newPost":
                    this.BlogID = this.inputParams[0].InnerText;
                    this.UserName = this.inputParams[1].InnerText;
                    this.Password = this.inputParams[2].InnerText;
                    this.Post = GetPost(this.inputParams[3]);
                    this.Publish = this.inputParams[4].InnerText != "0" && this.inputParams[4].InnerText != "false";

                    break;
                case "metaWeblog.editPost":
                    this.PostID = this.inputParams[0].InnerText;
                    this.UserName = this.inputParams[1].InnerText;
                    this.Password = this.inputParams[2].InnerText;
                    this.Post = GetPost(this.inputParams[3]);
                    this.Publish = this.inputParams[4].InnerText != "0" && this.inputParams[4].InnerText != "false";

                    break;
                case "metaWeblog.getPost":
                    this.PostID = this.inputParams[0].InnerText;
                    this.UserName = this.inputParams[1].InnerText;
                    this.Password = this.inputParams[2].InnerText;
                    break;
                case "metaWeblog.newMediaObject":
                    this.BlogID = this.inputParams[0].InnerText;
                    this.UserName = this.inputParams[1].InnerText;
                    this.Password = this.inputParams[2].InnerText;
                    this.MediaObject = GetMediaObject(this.inputParams[3]);
                    break;
                case "metaWeblog.getCategories":
                case "wp.getAuthors":
                case "wp.getPageList":
                case "wp.getPages":
                case "wp.getTags":
                    this.BlogID = this.inputParams[0].InnerText;
                    this.UserName = this.inputParams[1].InnerText;
                    this.Password = this.inputParams[2].InnerText;
                    break;
                case "metaWeblog.getRecentPosts":
                    this.BlogID = this.inputParams[0].InnerText;
                    this.UserName = this.inputParams[1].InnerText;
                    this.Password = this.inputParams[2].InnerText;
                    this.NumberOfPosts = Int32.Parse(this.inputParams[3].InnerText, CultureInfo.InvariantCulture);
                    break;
                case "blogger.getUsersBlogs":
                case "metaWeblog.getUsersBlogs":
                    this.AppKey = this.inputParams[0].InnerText;
                    this.UserName = this.inputParams[1].InnerText;
                    this.Password = this.inputParams[2].InnerText;
                    break;
                case "blogger.deletePost":
                    this.AppKey = this.inputParams[0].InnerText;
                    this.PostID = this.inputParams[1].InnerText;
                    this.UserName = this.inputParams[2].InnerText;
                    this.Password = this.inputParams[3].InnerText;
                    this.Publish = this.inputParams[4].InnerText != "0" && this.inputParams[4].InnerText != "false";

                    break;
                case "blogger.getUserInfo":
                    this.AppKey = this.inputParams[0].InnerText;
                    this.UserName = this.inputParams[1].InnerText;
                    this.Password = this.inputParams[2].InnerText;
                    break;
                case "wp.newPage":
                    this.BlogID = this.inputParams[0].InnerText;
                    this.UserName = this.inputParams[1].InnerText;
                    this.Password = this.inputParams[2].InnerText;
                    this.Page = GetPage(this.inputParams[3]);
                    this.Publish = this.inputParams[4].InnerText != "0" && this.inputParams[4].InnerText != "false";

                    break;
                case "wp.getPage":
                    this.BlogID = this.inputParams[0].InnerText;
                    this.PageID = this.inputParams[1].InnerText;
                    this.UserName = this.inputParams[2].InnerText;
                    this.Password = this.inputParams[3].InnerText;
                    break;
                case "wp.editPage":
                    this.BlogID = this.inputParams[0].InnerText;
                    this.PageID = this.inputParams[1].InnerText;
                    this.UserName = this.inputParams[2].InnerText;
                    this.Password = this.inputParams[3].InnerText;
                    this.Page = GetPage(this.inputParams[4]);
                    this.Publish = this.inputParams[5].InnerText != "0" && this.inputParams[5].InnerText != "false";

                    break;
                case "wp.deletePage":
                    this.BlogID = this.inputParams[0].InnerText;
                    this.UserName = this.inputParams[1].InnerText;
                    this.Password = this.inputParams[2].InnerText;
                    this.PageID = this.inputParams[3].InnerText;
                    break;
                default:
                    throw new MetaWeblogException("02", $"Unknown Method. ({MethodName})");
            }
        }

        /*
                /// <summary>
                /// The log meta weblog call.
                /// </summary>
                /// <param name="message">
                /// The message.
                /// </param>
                private void LogMetaWeblogCall(string message)
                {
                    var saveFolder = HttpContext.Current.Server.MapPath(BlogSettings.Instance.StorageLocation);
                    var saveFile = Path.Combine(saveFolder, "lastmetaweblogcall.txt");

                    try
                    {
                        // Save message to file
                        using (var fileWrtr = new FileStream(saveFile, FileMode.OpenOrCreate, FileAccess.Write))
                        {
                            using (var streamWrtr = new StreamWriter(fileWrtr))
                            {
                                streamWrtr.WriteLine(message);
                            }
                        }
                    }
                    catch
                    {
                        // Ignore all errors
                    }
                }
        */

        /// <summary>
        /// Retrieves the content of the input stream
        ///     and return it as plain text.
        /// </summary>
        /// <param name="context">
        /// The context.
        /// </param>
        /// <returns>
        /// The parse request.
        /// </returns>
        private static string ParseRequest(HttpContext context)
        {
            var buffer = new byte[context.Request.InputStream.Length];

            context.Request.InputStream.Position = 0;
            context.Request.InputStream.Read(buffer, 0, buffer.Length);

            return Encoding.UTF8.GetString(buffer);
        }

        #endregion
    }
}
