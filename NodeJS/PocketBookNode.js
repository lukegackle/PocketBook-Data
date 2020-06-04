//---------------------------------------
//-------Get bank account balances-------
//--------Written by: Luke Gackle--------
//---------------------------------------
//--This Nodejs Google Cloud function  --
//--logs into your pocketbook account  --
//--and scrapes the page for your bank --
//--account balances.                  --
//---------------------------------------




/**
 * Responds to any HTTP request.
 *
 * @param {!express:Request} req HTTP request context.
 * @param {!express:Response} res HTTP response context.
 */ 
exports.PocketBook = (req, res) => {
  //const escapeHtml = require('escape-html'); for debugging only
  let rp = require('request-promise').defaults({jar: true});
  var cheerio = require('cheerio'); // Basically jQuery for node.js
  var tough = require('tough-cookie');
  
  //options for initial request to homepage set for cookies
  let initialReqOp = {
    method: 'GET',
    uri: 'https://getpocketbook.com',
 	port: 443,
    resolveWithFullResponse: true
    };
	
  rp(initialReqOp)
    .then(function (response1) {
		
		//Get Set-cookies header and add cookies
	    var setcookies = response1.headers["set-cookie"];
    	var dt = Date.now() + 10000;
    	
    	if(setcookies != null){
          setcookies.forEach(function(cookie) {
                rp.jar().setCookie(cookie, 'https://getpocketbook.com', {expires: dt });  
          });
        }
    
    	//options for request to signin page
        let SignInReq = {
          method: 'GET',
          uri: 'https://getpocketbook.com/signin',
          port: 443,
          path: '/signin',
          resolveWithFullResponse: true
        };

		rp(SignInReq)
		  .then(function (response2) {
			//Setting variables for security features (CSRF)
			var $ = cheerio.load(response2.body);
				
			var csrf = $("meta[name='_csrf']").attr("content");
			var csrfHeader = $("meta[name='_csrf_header']").attr("content");
			var csrfparam = $("meta[name='_csrf_parameter']").attr("content");
			   
          	var setcookies2 = response2.headers["set-cookie"];

          	setcookies2.forEach(function(cookie) {
              rp.jar().setCookie(cookie, 'https://getpocketbook.com', {expires: dt });  
            });
					
			let options = {
				method: 'POST',
				uri: 'https://getpocketbook.com/login',
				port: 443,
				form: {
					// Like <input type="text" name="name">
					username: 'YOUR_USERNAME ',
					password: 'YOUR_PASSWORD',
					[csrfparam]: csrf,
					_remember_me: 'on'
				},
				headers: {
					[csrfHeader]: csrf
				}
			};

			rp(options)
    		  .then(function (body) {
          		res.status(200).send("Success?" + escapeHtml(body));
    		  })
   			  .catch(function (err) {
              	
              	if(err.statusCode == 302){
                  
                  //options for request to overview page
                  let OverViewReq = {
                    method: 'GET',
                    uri: 'https://getpocketbook.com/overview',
                    port: 443,
                    path: '/overview',
                    resolveWithFullResponse: true
                  };

                  rp(OverViewReq)
                  .then(function (body2) {
                    
                    //Get Data
                    var $$ = cheerio.load(body2.body);
                    
                    var table = $$("div:nth-child(3) > table");
                    
                    var BankAccount1 = table.find("tr:nth-child(2) > td:nth-child(2) > span").text();
                    var BankAccount2 = table.find("tr:nth-child(3) > td:nth-child(2) > span").text();
                    var BankAccount3 = table.find("tr:nth-child(4) > td:nth-child(2) > span").text();
                    var BankAccount4 = table.find("tr:nth-child(5) > td:nth-child(2) > span").text();
                    
                    BankAccount1 = BankAccount1.replace('$', '');
                    BankAccount1 = BankAccount1.replaceAll(',', '');
                    BankAccount1 = BankAccount1.replaceAll('\n', '');
                    
                    BankAccount2 = BankAccount2.replaceAll('$', '');
                    BankAccount2 = BankAccount2.replaceAll(',', '');
                    BankAccount2 = BankAccount2.replaceAll('\n', '');
                    
                    BankAccount3 = BankAccount3.replaceAll('$', '');
                    BankAccount3 = BankAccount3.replaceAll(',', '');
                    BankAccount3 = BankAccount3.replaceAll('\n', '');
                    
                    BankAccount4 = BankAccount4.replaceAll('$', '');
                    BankAccount4 = BankAccount4.replaceAll(',', '');
                    BankAccount4 = BankAccount4.replaceAll('\n', '');

                   
                    res.status(200).send("BankAccount1," + BankAccount1 + "\nBankAccount2," + BankAccount2 + "\nBankAccount3," + BankAccount3 + "\nBankAccount4," + BankAccount4);
                    
                    //Sign out
                 	
                    var csrf = $$("meta[name='_csrf']").attr("content");
					var csrfHeader = $$("meta[name='_csrf_header']").attr("content");
					var csrfparam = $$("meta[name='_csrf_parameter']").attr("content");
                    
                    //options for request to signout page
                    let SignOutReq = {
                      method: 'POST',
                      uri: 'https://getpocketbook.com/do-signout',
                      port: 443,
                      path: '/do-signout',
                      resolveWithFullResponse: true,
                      form: {
                        [csrfparam]: csrf
                      },
                      headers: {
                          [csrfHeader]: csrf
                      }
                  	};
                    
                    rp(SignOutReq)
                  	.then(function (body3) {
                      
                    })
                    .catch(function (err) {
                      //Server returns 302 redirect meaning signout successfull
                      if(err.statusCode == 302){
                      	var setcookies3 = err["response"]["headers"]["set-cookie"];
						if(setcookies3 != null){
                          setcookies3.forEach(function(cookie) {
                            rp.jar().setCookie(cookie, 'https://getpocketbook.com', {expires: new Date(0) });  
                          }); 
                        }
                        
                       	//Clear any leftover cookies
                        rp.jar().setCookie('JSESSIONID', 'https://getpocketbook.com', {expires: new Date(0) });
                        rp.jar().setCookie('RSESSIONID', 'https://getpocketbook.com', {expires: new Date(0) });
                        rp.jar().setCookie('USERLOGID', 'https://getpocketbook.com', {expires: new Date(0) });
                        rp.jar().setCookie('__cfduid', 'https://getpocketbook.com', {expires: new Date(0) });
                        rp.jar().setCookie('AWSELB', 'https://getpocketbook.com', {expires: new Date(0) });
                      }

                 	});
                    
                  })
                  .catch(function (err) {
                    res.status(200).send("Error Catch Statement: " + err);
                  });
                  
                  
                  
                  
                }
              else{
                res.status(200).send("Error Catch Statement: " + err);
              }
              	
    		  });
			
		  })
		  .catch(function (err) {
			// Crawling failed...
			res.status(200).send("Failed at signin Req, Error: " + err + " Headers: " + JSON.stringify(err.headers));
		  });
	   
	   
    })
    .catch(function (err) {
        // Crawling failed...
    	res.status(200).send("Failed at initial Req, Error: " + err + " Headers: " + JSON.stringify(err.headers));
    });
  

  
     String.prototype.replaceAll = function(search, replacement) {
    var target = this;
    return target.split(search).join(replacement);
};
  
};
