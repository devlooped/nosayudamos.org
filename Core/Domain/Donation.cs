namespace NosAyudamos
{
    public class Donation
    {
        public Donation(string donorId, string doneeId, string requestId, int amount)
            => (DonorId, DoneeId, RequestId, Amount)
            = (donorId, doneeId, requestId, amount);

        public string DonorId { get; }
        public string DoneeId { get; }
        public string RequestId { get; }
        public int Amount { get; }
    }
}
