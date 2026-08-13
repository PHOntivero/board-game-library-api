namespace BoardGameLibrary.Application.Common;

public static class ErrorCodes
{
    public static class Common
    {
        public const string ValidationFailed = "validation_failed";
        public const string BusinessRuleConflict = "business_rule_conflict";
        public const string UnexpectedError = "unexpected_error";
    }

    public static class BoardGames
    {
        public const string NotFound = "board_game_not_found";
        public const string Inactive = "inactive_board_game";
        public const string HasCopies = "board_game_has_copies";
    }

    public static class Categories
    {
        public const string NotFound = "category_not_found";
        public const string DuplicateName = "duplicate_category_name";
        public const string Inactive = "inactive_category";
        public const string HasBoardGames = "category_has_board_games";
    }

    public static class GameCopies
    {
        public const string NotFound = "game_copy_not_found";
        public const string DuplicateInventoryCode = "duplicate_inventory_code";
        public const string Inactive = "inactive_game_copy";
        public const string Damaged = "damaged_game_copy";
        public const string Unavailable = "game_copy_unavailable";
        public const string HasOpenLoan = "game_copy_has_open_loan";
        public const string HasLoanHistory = "game_copy_has_loan_history";
    }

    public static class Members
    {
        public const string NotFound = "member_not_found";
        public const string DuplicateMemberNumber = "duplicate_member_number";
        public const string DuplicateEmail = "duplicate_member_email";
        public const string Inactive = "inactive_member";
        public const string LoanLimitReached = "loan_limit_reached";
        public const string HasOverdueLoan = "member_has_overdue_loan";
        public const string HasLoanHistory = "member_has_loan_history";
    }

    public static class Loans
    {
        public const string NotFound = "loan_not_found";
        public const string AlreadyReturned = "loan_already_returned";
    }
}
