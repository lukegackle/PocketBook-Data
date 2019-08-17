//---------------------------------------
//-------Get Bank account balances-------
//--------Written by: Luke Gackle--------
//---------------------------------------
//-This C# Azure function logs into your-
//--pocketbook account and scrapes the --
//--page for your bank account balances--
//---------------------------------------

#r "Newtonsoft.Json"
#r "D:\home\site\wwwroot\bin\HtmlAgilityPack.dll"
//Get NuGet package and place .dll in Bin folder
//Azure Functions project.proj dependency definitions dont appear to be working

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
using HtmlAgilityPack;
 

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
            new KeyValuePair<string, string>("username", "username/email"),
            new KeyValuePair<string, string>("password", "password"),
            new KeyValuePair<string, string>(metaInformation["_csrf_parameter"], metaInformation["_csrf"]),
            new KeyValuePair<string, string>("_remember_me", "on"),
            
        });

    client.DefaultRequestHeaders.Add(metaInformation["_csrf_header"], metaInformation["_csrf"]);
    var loginResult = client.PostAsync("/login", content).Result;
    loginResult.EnsureSuccessStatusCode();


    var doc = new HtmlDocument();
    doc.LoadHtml(loginResult.Content.ReadAsStringAsync().Result);

   // With XPath
    var BankAccount1 = doc.DocumentNode.SelectSingleNode("//tr[2]/td[2]/span[1]").InnerHtml;
    var BankAccount2 = doc.DocumentNode.SelectSingleNode("//tr[3]/td[2]/span[1]").InnerHtml;
    var BankAccount3 = doc.DocumentNode.SelectSingleNode("//tr[4]/td[2]/span[1]").InnerHtml;
    var BankAccount4 = doc.DocumentNode.SelectSingleNode("//tr[5]/td[2]/span[1]").InnerHtml;

    BankAccount2=BankAccount2.Replace(",","");
    BankAccount2=BankAccount2.Replace("$","");

    BankAccount1=BankAccount1.Replace(",","");
    BankAccount1=BankAccount1.Replace("$","");

    BankAccount3=BankAccount3.Replace(",","");
    BankAccount3=BankAccount3.Replace("$","");

    BankAccount4=BankAccount4.Replace(",","");
    BankAccount4=BankAccount4.Replace("$","");

    return (ActionResult)new OkObjectResult( "BankAccount1," + BankAccount1 + "\nBankAccount2," + BankAccount2 + "\nBankAccount3,"+BankAccount3+"\nBankAccount4," + BankAccount4);
}
    
}
