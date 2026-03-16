namespace TaxiDispatch.DTOs.User.Responses
{
    public class UsersListResponseDto
    {
        public IList<ListedUser> Users { get; set; }

        public UsersListResponseDto()
        {
            Users = new List<ListedUser>();
        }
    }
}
