namespace Payment.Application.DTOs;

public class PaytrInitRequest
{
    public Guid OrderId { get; set; }
    public Guid UserId { get; set; }
    public string UserEmail { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public string UserPhone { get; set; } = string.Empty;
    public string UserAddress { get; set; } = string.Empty;
    public string UserIp { get; set; } = string.Empty;
    public decimal TotalAmount { get; set; }
    public string Currency { get; set; } = "TL";
    public List<PaytrBasketItem> BasketItems { get; set; } = new();
    public string MerchantOid { get; set; } = string.Empty;
    public int Installment { get; set; } = 0; // 0 = no installment
    public bool NoInstallment { get; set; } = true;
    public int MaxInstallment { get; set; } = 0;
}

public class PaytrBasketItem
{
    public string Name { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public int Quantity { get; set; }
}

