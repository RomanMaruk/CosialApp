using System.Diagnostics;
using System.Threading.Tasks;
using CircleApp.Data.Models;
using CosialApp.Data;
using CosialApp.ViewModel.Home;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CosialApp.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly AppDbContext _context;

        public HomeController(ILogger<HomeController> logger, AppDbContext context)
        {
            _logger = logger;
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var allPosts = await _context.Posts
                .Include(p => p.User)
                .Include(n => n.Likes)
                .OrderByDescending(n => n.DateCreated)
                .ToListAsync();
            return View(allPosts);
        }

        [HttpPost]
        public async Task<IActionResult> CreatePost(PostVM post)
        {
            // Get UserId
            int loggedUser = 1;

            var newPost = new Post
            {
                Content = post.Content,
                DateCreated = DateTime.Now,
                DateUpdated = DateTime.Now,
                ImageUrl = "",
                UserId = loggedUser,
                NrOfReports = 0
            };

            // Check and save image
            if(post.Image != null && post.Image.Length > 0)
            {
                string rootFolderPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
                if(post.Image.ContentType.Contains("image"))
                {
                    string rootFolderPathImages = Path.Combine(rootFolderPath, "images/uploadedPost/");
                    Directory.CreateDirectory(rootFolderPathImages);

                    string fileName = Guid.NewGuid().ToString() + Path.GetExtension(post.Image.FileName);
                    string filePath = Path.Combine(rootFolderPathImages, fileName);

                    using(var stream = new FileStream(filePath, FileMode.Create))
                        await post.Image.CopyToAsync(stream);
                    newPost.ImageUrl = "/images/uploadedPost/" + fileName;
                }
            }

            await _context.AddAsync(newPost);
            await _context.SaveChangesAsync();

            //Redirect to the index page
            return RedirectToAction("Index");
        }

        [HttpPost]
        public async Task<IActionResult> TogglePostLike(PostLikeVM postLikeVM)
        {
            int loggedInUser = 1;

            // Check if user has already liked the post 
            var like = await _context.Likes
                .Where(l =>  l.PostId == postLikeVM.PostId && l.UserId == loggedInUser )
                .FirstOrDefaultAsync();

            if (like != null)
            { 
                _context.Likes.Remove(like);
                await _context.SaveChangesAsync();
            } 
            else
            {
                var newLike = new Like()
                {
                    PostId = postLikeVM.PostId,
                    UserId = loggedInUser,
                };
                _context.Likes.Add(newLike);
                await _context.SaveChangesAsync();
            }

            return RedirectToAction("Index");
        }
    }
}
