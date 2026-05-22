using Linkedin.Core.Data;
using Linkedin.Core.Entities;
using Linkedin.DataAccess.Repositories.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Linkedin.DataAccess.Repositories.Concrete
{
    public class UnitOfWork : IUnitOfWork
    {
        private readonly AppDbContext _context;
        public IUserRepository Users { get; private set; }
        public IPostRepository Posts { get; private set; }
        public IChatRepository Chats { get; private set; }
        public ICommentRepository Comments { get; private set; }
        public ILikeRepository Likes { get; private set; }
        public IRefreshToken RefreshTokens { get; private set; }


        public INotificationsRepository Notifications { get; private set; }

        public IMessageRepository Messages { get; private set; }

        public IExperienceRepository Experiences { get; private set; }

        public IEducationRepository Educations { get; private set; }

        public IUserSkillRepository Skills { get; private set; }

        public IConnectionRepository Connections { get; private set; }

        public IConnectionRequestRepository ConnectionRequests { get; private set; }
        public IJobPostRepository JobPosts { get; private set; }
        public ISavedJobRepository SavedJobs { get; private set; }

        public IJobApplicationRepository JobApplications { get; private set; }
        public ICompanyFollowRepository CompanyFollows { get; private set; }
        public UnitOfWork(AppDbContext context)
        {
            _context = context;
            Users = new UserRepository(context);
            Posts = new PostRepository(context);
            Chats = new ChatRepository(context);
            Comments= new CommentRepository(context);
            Likes=new LikeRepository(context);
            RefreshTokens= new RefreshTokenRepository(context);
            Notifications=new NotificationsRepositor(context);  
            Messages = new MessageRepository(context);
            Experiences = new ExperienceRepository(context);
            Educations = new EducationRepository(context);
            Skills = new UserSkillRepository(context);
            Connections = new ConnectionRepository(context);
            ConnectionRequests = new ConnectionRequestRepository(context);
            SavedJobs = new SavedJobRepository(context);
            JobApplications = new JobApplicationRepository(context);
            JobPosts = new JobPostRepository(context);
            CompanyFollows = new CompanyFollowRepository(context);

        }

        public async Task<int> CompleteAsync() => await _context.SaveChangesAsync();
        public void Dispose() => _context.Dispose();


    }
}
