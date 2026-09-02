namespace TransactionService.Dtos;

public record TransferRequest(int FromAccountId, int ToAccountId, decimal Amount);