//---------------------------------------
//-------Get Bank account balances-------
//--------Written by: Luke Gackle--------
//---------------------------------------
//-This C# Azure function logs into your-
//--pocketbook account and scrapes the --
//--page for your bank account balances--
//---------------------------------------


#r "Newtonsoft.Json"

using System;
using System.Security;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Primitives;
using System.Collections.Generic;
using Newtonsoft.Json;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.XPath;


public static async Task<IActionResult> Run(HttpRequest req, ILogger log)
{
  var baseAddress = new Uri("https://getpocketbook.com");
    var cookieContainer = new CookieContainer();
    using (var handler = new HttpClientHandler() { CookieContainer = cookieContainer })
    using (var client = new HttpClient(handler) { BaseAddress = baseAddress })
    {
		//usually i make a standard request without authentication, eg: to the home page.
        //by doing this request you store some initial cookie values, that might be used in the subsequent login request and checked by the server
        var homePageResult = client.GetAsync("/");
        homePageResult.Result.EnsureSuccessStatusCode();

        var signinPageResult = client.GetAsync("/signin").Result;
        signinPageResult.EnsureSuccessStatusCode();

        //Get Meta Tags for CSRF

        Regex metaTag = new Regex("<meta name=\"(.+?)\" content=\"(.+?)\"/>");
        Dictionary<string, string> metaInformation = new Dictionary<string, string>();

        foreach(Match m in metaTag.Matches(signinPageResult.Content.ReadAsStringAsync().Result)) {
            metaInformation.Add(m.Groups[1].Value, m.Groups[2].Value);
        } 

        var content = new FormUrlEncodedContent(new[]
        {
            //the name of the form values must be the name of <input /> tags of the login form, in this case the tag is <input type="text" name="username">
            new KeyValuePair<string, string>("username", "EMAIL USERNAME"),
            new KeyValuePair<string, string>("password", "PASSWORD"),
            new KeyValuePair<string, string>(metaInformation["_csrf_parameter"], metaInformation["_csrf"]),
            new KeyValuePair<string, string>("_remember_me", "on"),
            
        });

	//Add CSRF to the headers
    client.DefaultRequestHeaders.Add(metaInformation["_csrf_header"], metaInformation["_csrf"]);
	
	//Submit the information to the login page
    var loginResult = client.PostAsync("/login", content).Result;
    loginResult.EnsureSuccessStatusCode();

	//Removing content that does not comply with XML standards such as scripts, images that havent been closed off, empty attributes, incorrect tag placement, and special characters
    Regex rRemScript = new Regex(@"<script[^>]*>[\s\S]*?</script>");
    var output = rRemScript.Replace(loginResult.Content.ReadAsStringAsync().Result, "");
    output= output.Replace("<link rel=\"shortcut icon\" href=\"/wp-content/themes/pocketbook/img/favicon.ico\">", "");
    output= output.Replace("<link rel=\"stylesheet\" href=\"/assets/css/font-awesome-ie7.min.css?8a684150f33dcd2b00258dd04bab6eb6b3150c74\">", "");
    output= output.Replace("<link rel=\"apple-touch-icon-precomposed\" href=\"/assets/img/apple-touch-icon.png\">", "");
    output= output.Replace("<meta charset=\"utf-8\">", "");
    output= output.Replace("<img src=\"https://getpocketbook.com/wp-content/themes/pocketbook/img/logo-1.png\" height=\"30\" width=\"30\" alt=\"Pocketbook logo\">", "");
    output= output.Replace("<img src=\"https://s3-ap-southeast-2.amazonaws.com/pb-static-prod/wp/footer-pb-logo.svg\">", "");
    output= output.Replace("&nbsp;", "");
    output= output.Replace("&times;", "");
    output= output.Replace("&amp;", "");
    output= output.Replace("<form id=\"addCashForm\" class=\"center\" class=\"well\" action=\"/transaction/add\" method=\"POST\">","<form id=\"addCashForm\" action=\"/transaction/add\" method=\"POST\">");
    output= output.Replace("<input id=\"cashTransactionDate\" placeholder=\"Today\" type=\"text\" class=\"input-small\" readonly","<input id=\"cashTransactionDate\" placeholder=\"Today\" type=\"text\" class=\"input-small\"");
    output= output.Replace("<img height=\"1\" width=\"1\" alt=\"\" style=\"display:none\" src=\"https://www.facebook.com/tr?id=680652438676474&ev=NoScript\" />","");
    output= output.Replace("<small><strong>Spent: </small><span class=\"amount\">$385.26</span></strong><br/>", "");
	output= output.Replace("<small><strong>Earned: </small><span class=\"amount green\">$480.45</span></strong><br/>", "");
    XmlDocument doc = new XmlDocument();
    doc.LoadXml(output);
    
	//Selecting the bank account balance data from the page, tweak this based on how many bank accounts you have, hopefully the XPATH selectors will be the same
    var ANCh = doc.SelectSingleNode("//tr[2]/td[2]/span[1]").InnerText;
    var ANOn = doc.SelectSingleNode("//tr[3]/td[2]/span[1]").InnerText;
    var ANPr = doc.SelectSingleNode("//tr[4]/td[2]/span[1]").InnerText;
    var MACm = doc.SelectSingleNode("//tr[5]/td[2]/span[1]").InnerText;

    ANOn=ANOn.Replace(",","");
    ANOn=ANOn.Replace("$","");

    ANCh=ANCh.Replace(",","");
    ANCh=ANCh.Replace("$","");

    ANPr=ANPr.Replace(",","");
    ANPr=ANPr.Replace("$","");

    MACm=MACm.Replace(",","");
    MACm=MACm.Replace("$","");

	//Create and return the responce
    return (ActionResult)new OkObjectResult( "{\"ANCh\":\"" + ANCh + "\", \"ANOn\":\"" + ANOn + "\", \"ANPr\":\""+ANPr+"\", \"MACm\":\"" + MACm +"\"}");
    
}

    
}
