using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace ListenLater.Pages {
    public class Trigger : PageModel {
        [BindProperty]
        public string username { get; set; }
        
        public void OnGet() {
            
        }
        
        public IActionResult OnPost() {
            return Redirect($"api/download-new-videos/{username}");
        }
    }
}