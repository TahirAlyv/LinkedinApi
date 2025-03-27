using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LinkedIn.Core.Entities
{
     public class Follow
   {
       public int Id { get; set; }
       public string FollowerId { get; set; }
       public string FollowingId { get; set; }
       public ApplicationUser Follower { get; set; }
       public ApplicationUser Following { get; set; }

   }
}
