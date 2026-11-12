namespace Ecommerce.Web.Models;

// Traduit le statut brut (anglais, venant d'Ordering.Api) en libellé et style d'affichage -
// centralisé ici pour ne pas répéter ce switch dans chaque vue qui affiche une commande.
public static class OrderStatusPresentation
{
    public static string Label(string status) => status switch
    {
        "PendingPayment" => "En attente de paiement",
        "Paid" => "Payée",
        "Cancelled" => "Annulée",
        "Refunded" => "Remboursée",
        _ => status
    };

    public static string CssClass(string status) => status switch
    {
        "PendingPayment" => "status-badge--pending",
        "Paid" => "status-badge--paid",
        "Cancelled" or "Refunded" => "status-badge--cancelled",
        _ => "status-badge--default"
    };
}
