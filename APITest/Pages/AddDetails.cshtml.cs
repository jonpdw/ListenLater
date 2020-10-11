using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Newtonsoft.Json;

namespace APITest.Pages {
    [BindProperties]
    public class AddDetails : PageModel {
        public string username { get; set; }
        public string overcast_username { get; set; }

        public string overcast_password { get; set; }
        public string youtube_cookies { get; set; }
        public string pushover_token { get; set; }
        public string pushover_user { get; set; }

        public void OnGet() {
        }

        public IActionResult OnPost() {
            Console.WriteLine("Posted");
            UpdateJSON();
            System.IO.File.WriteAllText($"cookies/{username}.txt", youtube_cookies); 
            return RedirectToPage();
        }

        private void UpdateJSON() {
            var userDetailsText = System.IO.File.ReadAllText("UserDetails.json");
            var userDet =
                JsonConvert.DeserializeObject<Dictionary<string, Dictionary<string, Dictionary<string, string>>>>(
                    userDetailsText);
            userDet[username] = new Dictionary<string, Dictionary<string, string>> {
                {
                    "Overcast",
                    new Dictionary<string, string> {
                        {"username", overcast_username},
                        {"password", overcast_password}
                    }
                }, {
                    "Pushover",
                    new Dictionary<string, string> {
                        {"token", pushover_token},
                        {"user", pushover_user}
                    }
                }
            };
            string json = JsonConvert.SerializeObject(userDet, Formatting.Indented);
            System.IO.File.WriteAllText("UserDetails.json", json);
        }
    }
}