using SolPlay.Scripts.Services;

namespace SolPlay.Scripts.Ui
{
    public class TransferPopupUiData : UiService.UiData
    {
        public SolPlayNft NftToTransfer;

        public TransferPopupUiData(SolPlayNft solPlayNft)
        {
            NftToTransfer = solPlayNft;
        }
    }
}