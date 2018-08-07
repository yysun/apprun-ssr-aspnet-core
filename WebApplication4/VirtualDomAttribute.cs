using cloudscribe.HtmlAgilityPack;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Filters;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WebApplication4
{
    public class VirtualDomAttribute : ResultFilterAttribute
    {
        Stream newStream;
        Stream oldStream;
        bool isSSR;

        public override void OnResultExecuting(ResultExecutingContext filterContext)
        {
            base.OnResultExecuting(filterContext);
            var accept = filterContext.HttpContext.Request.Headers["accept"];
            this.isSSR = !accept.Any(s => s.Contains("application/json"));
            oldStream = filterContext.HttpContext.Response.Body;
            newStream = new MemoryStream();
            filterContext.HttpContext.Response.Body = newStream;
        }

        public override void OnResultExecuted(ResultExecutedContext filterContext)
        {
            newStream.Seek(0, SeekOrigin.Begin);
            using (var streamReader = new StreamReader(newStream, Encoding.UTF8, true, 512, true))
            {
                var capturedText = streamReader.ReadToEnd();
                var vdom = capturedText;
                if (!this.isSSR)
                {
                    var doc = new HtmlDocument();
                    doc.LoadHtml(capturedText);
                    var root = doc.DocumentNode.SelectSingleNode("//div[@id='apprun-app']");
                    if (root == null) root = doc.DocumentNode.SelectSingleNode("//div");
                    vdom = RemoveWhiteSpace(Convert(root).GetValue("children").ToString(Formatting.None));
                }
                filterContext.HttpContext.Response.Body = oldStream;
                filterContext.HttpContext.Response.WriteAsync(vdom);
            }
        }


        string RemoveWhiteSpace(string s)
        {
            return s.Replace("\\r", "").Replace("\\n", "").Trim();
        }

        public JObject Convert(HtmlNode documentNode)
        {
            if (documentNode.Name == "#comment") return null;
            if (documentNode.Name == "#document") documentNode.Name = "div";
            var children = new JArray();
            foreach (var child in documentNode.ChildNodes)
            {
                if (child.Name == "#text")
                {
                    if (RemoveWhiteSpace(child.InnerText).Length > 0)
                    {
                        children.Add(new JValue(HtmlEntity.DeEntitize(child.InnerText)));
                    }
                }
                else
                {
                    var ch = Convert(child);
                    if (ch != null) children.Add(ch);
                }
            }
            var vdom = JObject.FromObject(new
            {
                tag = documentNode.Name,
                children = children
            });
            var props = JObject.FromObject(new {});
            documentNode.Attributes.ToList().ForEach(attr =>
            {
                var name = attr.Name;
                if (name == "class") name = "className";
                props.Add(name, attr.Value);
            });
            if(props.HasValues) vdom.Add("props", props);
            return vdom;              
        }
    }
    
}