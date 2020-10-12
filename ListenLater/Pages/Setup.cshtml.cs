using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Newtonsoft.Json;

namespace ListenLater.Pages {
    [BindProperties]
    public class AddDetails : PageModel {
        public Boolean runEvery30Min { get; set; }
        public string username { get; set; }
        public string overcast_username { get; set; }

        public string overcast_password { get; set; }
        public string youtube_cookies { get; set; }
        public string pushover_user { get; set; }

        public void OnGet() {
        }

        public IActionResult OnPost() {
            Console.WriteLine("Posted");
            UpdateJSON();
            System.IO.File.WriteAllText($"user-data/cookies/{username}.txt", youtube_cookies); 
            AddToRunEvery30MinsFile();
            return RedirectToPage();
        }

        private void AddToRunEvery30MinsFile() {
            if (runEvery30Min) {
                HashSet<string> lines = System.IO.File.ReadLines("user-data/RunEvery30Mins.txt").ToHashSet();
                lines.Add(username);
                System.IO.File.WriteAllText("user-data/RunEvery30Mins.txt", string.Join("\n", lines.ToArray()));
            }
            
            
        }

        private void UpdateJSON() {
            var userDetailsText = System.IO.File.ReadAllText("user-data/UserDetails.json");
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
                        {"user", pushover_user}
                    }
                },
            };
            string json = JsonConvert.SerializeObject(userDet, Formatting.Indented);
            System.IO.File.WriteAllText("user-data/UserDetails.json", json);
        }
    }
}