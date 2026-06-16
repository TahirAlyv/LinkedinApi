using Linkedin.Core.Data;
using Linkedin.Core.Dtos;
using Linkedin.Core.Entities;
using Linkedin.Core.Enums;
using Linkedin.DataAccess.Repositories.Interfaces;
 
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace Linkedin.DataAccess.Repositories.Concrete
{
    public class PostRepository : Repository<Post>, IPostRepository
    {
        public PostRepository(AppDbContext context) : base(context)
        {
        }

        public async Task<List<Post>> GetAllPostsByFriendIdsAsync(List<string> friendIds)
        {
            return await _context.Posts
                 .Where(p => friendIds.Contains(p.UserID) && !p.IsBlocked)
                .Include(p => p.User)
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync();
        }

        public async Task<Post?> GetUserPostAsync(string userId, int postId)
        {
            var post = await _context.Posts
                .Include(p => p.Comments)
                .Include(p => p.Likes)
                .Include(p => p.User)
                .FirstOrDefaultAsync(p => p.UserID == userId && p.Id == postId);

            return post;
        }

        public async Task<List<Post>> GetPostsByUserIdAsync(string userId, int skip, int take)
        {
            return await _context.Posts
                .Where(p => p.UserID == userId && !p.IsBlocked)
                .Include(p => p.Likes)
                .Include(p => p.Comments)
                .Include(p => p.User)
                    .ThenInclude(u => u.Company)
                .OrderByDescending(p => p.CreatedAt)
                .Skip(skip)
                .Take(take)
                .ToListAsync();
        }

        public async Task<List<Post>> GetHomeFeedPostsAsync(
            List<string> allowedUserIds,
            int skip,
            int take)
        {
            if (allowedUserIds == null || !allowedUserIds.Any())
                return new List<Post>();

            return await _context.Posts
                .AsNoTracking()
                 .Where(p => allowedUserIds.Contains(p.UserID) && !p.IsBlocked)
                .Include(p => p.Likes)
                .Include(p => p.Comments)
                .Include(p => p.User)
                    .ThenInclude(u => u.Company)
                .OrderByDescending(p => p.CreatedAt)
                .Skip(skip)
                .Take(take)
                .ToListAsync();
        }

        public async Task<Post?> GetPostByIdAsync(
            int postId,
            params Expression<Func<Post, object>>[] includes)
        {
            IQueryable<Post> query = _context.Posts;

            foreach (var include in includes)
            {
                query = query.Include(include);
            }

            return await query.FirstOrDefaultAsync(p => p.Id == postId);
        }


        public async Task<List<Post>> GetRecommendedFeedPostsAsync(
        string currentUserId,
        int page,
        int pageSize)
        {
            if (string.IsNullOrWhiteSpace(currentUserId))
                return new List<Post>();

            if (page < 1)
                page = 1;

            if (pageSize < 1)
                pageSize = 10;

            if (pageSize > 50)
                pageSize = 50;

            var currentUser = await _context.Users
                .AsNoTracking()
                .Include(u => u.Skills)
                .FirstOrDefaultAsync(u => u.Id == currentUserId);

            if (currentUser == null)
                return new List<Post>();

            var connectedUserIds = await _context.Connections
                .AsNoTracking()
                .Where(c => c.UserId == currentUserId)
                .Select(c => c.ConnectedUserId)
                .Distinct()
                .ToListAsync();

            var followedEmployerIds = await _context.CompanyFollows
                .AsNoTracking()
                .Where(cf => cf.FollowerId == currentUserId)
                .Select(cf => cf.EmployerId)
                .Distinct()
                .ToListAsync();

            var searchKeywords = await _context.SearchHistories
                .AsNoTracking()
                .Where(x => x.UserId == currentUserId)
                .OrderByDescending(x => x.CreatedAt)
                .Take(10)
                .Select(x => x.NormalizedQuery)
                .ToListAsync();

            searchKeywords = searchKeywords
                .SelectMany(x => ExtractKeywords(x))
                .Distinct()
                .ToList();

            var positionKeywords = ExtractKeywords(currentUser.CurrentPosition);
            var locationKeywords = ExtractKeywords(currentUser.Location);

            var skillKeywords = currentUser.Skills
                .Where(s => !string.IsNullOrWhiteSpace(s.Name))
                .SelectMany(s => ExtractKeywords(s.Name))
                .Distinct()
                .ToList();

            var candidatePosts = await _context.Posts
                .AsNoTracking()
                .Where(p =>
                    !p.IsBlocked &&
                    !p.User.IsBlocked &&
                    (
                        p.UserID == currentUserId ||
                        p.User.Visibility == Linkedin.Core.Enums.Visibility.Public ||
                        connectedUserIds.Contains(p.UserID)
                    )
                )
                .Include(p => p.Likes)
                .Include(p => p.Comments)
                .Include(p => p.User)
                    .ThenInclude(u => u.Company)
                .OrderByDescending(p => p.CreatedAt)
                .Take(500)
                .ToListAsync();

            var scoredPosts = candidatePosts
                .Select(p =>
                {
                    var searchableText = NormalizeText(
                        $"{p.Content} " +
                        $"{p.User.UserName} " +
                        $"{p.User.FullName} " +
                        $"{p.User.CurrentPosition} " +
                        $"{p.User.Location} " +
                        $"{p.User.Bio} " +
                        $"{p.User.Company?.Name} " +
                        $"{p.User.Company?.Industry} " +
                        $"{p.User.Company?.Location} " +
                        $"{p.User.Company?.Bio}"
                    );

                    var hasPositionMatch = positionKeywords.Any(k => searchableText.Contains(k));
                    var hasLocationMatch = locationKeywords.Any(k => searchableText.Contains(k));
                    var hasSkillMatch = skillKeywords.Any(k => searchableText.Contains(k));
                    var hasSearchMatch = searchKeywords.Any(k => searchableText.Contains(k));

                    var isOwnPost = p.UserID == currentUserId;
                    var isConnectedPost = connectedUserIds.Contains(p.UserID);
                    var isFollowedEmployerPost = followedEmployerIds.Contains(p.UserID);

                    var strongRelevant =
                        isOwnPost ||
                        isConnectedPost ||
                        isFollowedEmployerPost ||
                        hasSkillMatch ||
                        hasPositionMatch ||
                        hasSearchMatch ||
                        (hasLocationMatch && (hasPositionMatch || hasSkillMatch));

                    var weakRelevant =
                        hasLocationMatch ||
                        (currentUser.UserType == Linkedin.Core.Enums.UserType.JobSeeker &&
                         p.User.UserType == Linkedin.Core.Enums.UserType.Employer) ||
                        (currentUser.UserType == Linkedin.Core.Enums.UserType.Employer &&
                         p.User.UserType == Linkedin.Core.Enums.UserType.JobSeeker);

                    var score = 0;

                    if (isOwnPost)
                        score += 100;

                    if (isConnectedPost)
                        score += 40;

                    if (isFollowedEmployerPost)
                        score += 35;

                    if (hasSearchMatch)
                        score += 35;

                    if (hasSkillMatch)
                        score += 30;

                    if (hasPositionMatch)
                        score += 25;

                    if (hasLocationMatch)
                        score += 10;

                    if (hasPositionMatch && hasSkillMatch)
                        score += 35;

                    if (hasLocationMatch && hasPositionMatch)
                        score += 20;

                    if (hasLocationMatch && hasSkillMatch)
                        score += 15;

                    if (hasLocationMatch && hasPositionMatch && hasSkillMatch)
                        score += 60;

                    if (hasSearchMatch && hasSkillMatch)
                        score += 25;

                    if (hasSearchMatch && hasPositionMatch)
                        score += 25;

                    if (hasSearchMatch && hasLocationMatch && hasPositionMatch && hasSkillMatch)
                        score += 70;

                    if (strongRelevant || weakRelevant)
                    {
                        if (currentUser.UserType == Linkedin.Core.Enums.UserType.JobSeeker &&
                            p.User.UserType == Linkedin.Core.Enums.UserType.Employer)
                            score += 20;

                        if (currentUser.UserType == Linkedin.Core.Enums.UserType.Employer &&
                            p.User.UserType == Linkedin.Core.Enums.UserType.JobSeeker)
                            score += 20;

                        if (p.CreatedAt >= DateTime.UtcNow.AddDays(-7))
                            score += 10;
                        else if (p.CreatedAt >= DateTime.UtcNow.AddDays(-30))
                            score += 5;

                        var likeCount = p.Likes?.Count ?? (p.LikeCount ?? 0);
                        var commentCount = p.Comments?.Count ?? (p.CommentCount ?? 0);

                        score += Math.Min(likeCount + commentCount, 20);

                        score += (p.Id * 13) % 10;
                    }

                    return new
                    {
                        Post = p,
                        Score = score,
                        StrongRelevant = strongRelevant,
                        WeakRelevant = weakRelevant
                    };
                })
                .ToList();

            var strongPosts = scoredPosts
                .Where(x => x.StrongRelevant)
                .OrderByDescending(x => x.Score)
                .ThenByDescending(x => x.Post.CreatedAt)
                .ToList();

            var weakPosts = scoredPosts
                .Where(x => !x.StrongRelevant && x.WeakRelevant)
                .OrderByDescending(x => x.Score)
                .ThenByDescending(x => x.Post.CreatedAt)
                .Take(Math.Max(2, pageSize / 4))
                .ToList();

            var fallbackPosts = scoredPosts
                .Where(x => !x.StrongRelevant && !x.WeakRelevant)
                .OrderByDescending(x => x.Post.CreatedAt)
                .Take(Math.Max(1, pageSize / 5))
                .ToList();

            var finalPosts = strongPosts
                .Concat(weakPosts)
                .Concat(fallbackPosts)
                .GroupBy(x => x.Post.Id)
                .Select(g => g.First())
                .OrderByDescending(x => x.Score)
                .ThenByDescending(x => x.Post.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(x => x.Post)
                .ToList();

            return finalPosts;
        }

        private static string NormalizeText(string? text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return "";

            return text.Trim().ToLower();
        }

        private static List<string> ExtractKeywords(string? text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return new List<string>();

            char[] separators =
            {
            ' ', ',', '.', ';', ':', '-', '_', '/', '\\',
            '(', ')', '[', ']', '{', '}', '\n', '\r', '\t'
            };

            return text
                .ToLower()
                .Split(separators, StringSplitOptions.RemoveEmptyEntries)
                .Where(x => x.Length >= 2)
                .Distinct()
                .ToList();
        }
    }

}
 