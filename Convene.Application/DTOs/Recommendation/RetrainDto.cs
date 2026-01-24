using System;

namespace Convene.Application.DTOs.Recommendation
{
    public class RetrainDto
    {
        public Guid? UserId { get; set; }  //  If set, retrain for one user only
    }
}
