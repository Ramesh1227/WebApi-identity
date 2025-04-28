using Amazon.DynamoDBv2.DataModel;

namespace FaceAuthenticationsFunctions.Models
{
    [DynamoDBTable("ScheduledMeetingList")]
    public class ScheduledMeetingList
    {
        [DynamoDBHashKey]
        public string MeetingId { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public string OrganizerId { get; set; }
        public string RoomId { get; set; }
        public string RoomName { get; set; }
        public string ScheduledFrom { get; set; }
        public string ScheduledFromTime { get; set; }
        public string ScheduledTo { get; set; }
        public string ScheduledToTime { get; set; }
        public string IsApprovedbyAdmin { get; set; }
        public string MeetingStatus { get; set; }
        public List<string> AttendesList { get; set; }
    }
}
