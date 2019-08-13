# PocketBook-Data
This script will get data from your Pocket Book account and scrape the web page to return data that you need, the script was designed as an Azure functions app and can be easily deployed as a function app in Azure or tweaked with your own code.

The script will load the Pocket Book sign in page and submit the form data that you specified to the login page which will then take you to the homepage and return the HTML.

I then take that HTML and remove elements that do not comply with the XML standard, and then use XPath selectors to get the data I need from the page.

Feel free to submit any improvements you feel may improve the efficiency of this script.
