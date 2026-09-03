namespace TransactionServiceService.Models;

public class Transaction
{
    public int Id { get; set; }
    public int FromAccountIdAccountId {get; set;}
    public int ToAccountId {get; set;}
    public decimal Amount {get; set;}
    public string Status {get; set;} = "";
    public DateTime CreatedAt {get; set;}

}
