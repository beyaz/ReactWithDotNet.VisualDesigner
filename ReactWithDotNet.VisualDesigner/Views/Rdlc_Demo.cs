namespace ReactWithDotNet.VisualDesigner.Views;

static class Rdlc_Demo
{
    static List<string> GenerateHtml(AnyContract m, IReadOnlyList<AnyRecord> records)
    {
        #region Designer Generated Code

        return new LineCollection
        {
            "<html>",
            "    <body>",
            "        <div style=\"width: 793.70px; margin: 0 auto; background-color: #ffffff; box-shadow: rgba(0, 0, 0, 0.08) 0px 0px 7.55906px 0px; padding-top: 20px; padding-left: 64px; padding-right: 64px\">",
            "            <table style=\"width: 100%\">",
            "                <tr>",
            "                    <td>",
            "                        <img src=\"data:image/png;base64, iVBORw0KGgoAAAANSUhEUgAAAAUAAAAFCAYAAACNbyblAAAAHElEQVQI12P4//8/w38GIAXDIBKE0DHxgljNBAAO9TXL0Y4OHwAAAABJRU5ErkJggg==\" style=\"width: 177.65px; height: 81.5547px; object-fit: contain\">",
            "                    </td>",
            "                    <td style=\"width: 200px\">",
            "                    <td>",
            "                        <div style=\"background-color: #000; color: #fff; font-family: Verdana, Arial, sans-serif; font-weight: 700; padding: 5px 10px; white-space: nowrap\">",
            "KREDİ KARTI HESAP ÖZETİ",
            "                        </div>",
            "                    </td>",
            "                </tr>",
            "                <tr style=\"height: 50px\">",
            "            </table>",
            "            <table style=\"padding-top: 20px; width: 100%\">",
            "                <tr>",
            "                    <td style=\"width: 300px; vertical-align: top\">",
            "                        <table style=\"width: 100%\">",
            "                            <tr>",
            "                                <td colSpan=2>",
            "                                    <span style=\"font-family: 'Arial Black', Arial, sans-serif; font-size: 10.6667px; color: #ff0000; font-weight: 700\">",
            "Son Ödeme Tarihi:",
            "                                    </span>",
            "                                </td>",
            "                                <td>",
            "                                    <span style=\"font-size: 10.6667px; font-weight: 800; margin-left: auto\">",
            m.ExpiryDateWithoutDayName,
            "                                    </span>",
            "                                </td>",
            "                            </tr>",
            "                            <tr>",
            "                                <td>",
            "                                    <div style=\"font-size: 10.6667px; font-weight: 800; margin-bottom: 10px\">",
            m.CustomerNameAndSurname,
            "                                    </div>",
            "                                </td>",
            "                            </tr>",
            "                            <tr>",
            "                                <td colSpan=2>",
            "                                    <div style=\"font-size: 10.6667px; font-weight: 700; white-space: pre-line\">",
            m.FullAddress,
            "                                    </div>",
            "                                </td>",
            "                            </tr>",
            "                        </table>",
            "                    </td>",
            "                    <td style=\"width: 100px\">",
            "                    <td style=\"width: 300px\">",
            "                        <div style=\"display: flex; flex-direction: column\">",
            "                            <div style=\"margin-bottom: 20px\">",
            "                                <div style=\"font-family: 'Arial Black', Arial, sans-serif; font-size: 10.6667px; color: #006f51; margin-bottom: 5px\">",
            "Kart ve Müşteri Bilgileriniz",
            "                                </div>",
            "                                <hr style=\"border-top: 1px solid #000; margin-bottom: 5px\">",
            "                                <table style=\"width: 100%; border-collapse: collapse; font-family: Arial, Helvetica, sans-serif\">",
            "                                    <tbody>",
            "                                        <tr>",
            "                                            <td style=\"font-weight: 700; color: #006f51; font-size: 10.6667px; padding: 2px\">",
            "Müşteri No",
            "                                            </td>",
            "                                            <td style=\"font-weight: 700; text-align: right; font-size: 10.6667px; padding: 2px\">",
            m.CustomerNo,
            "                                            </td>",
            "                                        </tr>",
            "                                        <tr>",
            "                                            <td style=\"font-weight: 700; color: #006f51; font-size: 10.6667px; padding: 2px\">",
            "Kart No",
            "                                            </td>",
            "                                            <td style=\"font-weight: 700; text-align: right; font-size: 10.6667px; padding: 2px\">",
            m.CardNumberHidden,
            "                                            </td>",
            "                                        </tr>",
            "                                        <tr>",
            "                                            <td style=\"font-weight: 700; color: #006f51; font-size: 10.6667px; padding: 2px\">",
            "Kart Limitiniz",
            "                                            </td>",
            "                                            <td style=\"font-weight: 700; text-align: right; font-size: 10.6667px; padding: 2px\">",
            $"{m.CreditLimitInland} TL",
            "                                            </td>",
            "                                        </tr>",
            "                                        <tr>",
            "                                            <td style=\"color: #ff0000; font-weight: 700; font-size: 10.6667px; padding: 2px\">",
            "Kullanılabilir Limitiniz",
            "                                            </td>",
            "                                            <td style=\"font-weight: 700; text-align: right; font-size: 10.6667px; padding: 2px\">",
            $"{m.RemainCreditLimitInland} TL",
            "                                            </td>",
            "                                        </tr>",
            "                                        <tr style=\"display: {ShowSegmentRow}\">",
            "                                            <td style=\"font-weight: 700; color: #006f51; font-size: 10.6667px; padding: 2px\">",
            "Kart Segmenti",
            "                                            </td>",
            "                                            <td style=\"font-weight: 700; text-align: right; font-size: 10.6667px; padding: 2px\">",
            m.MilesSmilesCardSegment,
            "                                            </td>",
            "                                        </tr>",
            "                                    </tbody>",
            "                                </table>",
            "                            </div>",
            "                            <div style=\"margin-bottom: 20px\">",
            "                                <div style=\"font-family: 'Arial Black', Arial, sans-serif; font-size: 10.6667px; color: #006f51; margin-bottom: 5px\">",
            "Hesap Bilgileriniz",
            "                                </div>",
            "                                <hr style=\"border-top: 1px solid #000; margin-bottom: 5px\">",
            "                                <table style=\"width: 100%; border-collapse: collapse; font-family: Arial, Helvetica, sans-serif\">",
            "                                    <tbody>",
            "                                        <tr>",
            "                                            <td style=\"font-weight: 700; color: #006f51; font-size: 10.6667px; padding: 2px\">",
            "Hesap Kesim Tarihi",
            "                                            </td>",
            "                                            <td style=\"font-weight: 700; text-align: right; font-size: 10.6667px; padding: 2px\">",
            m.CreateDateWithoutDayNameRepl,
            "                                            </td>",
            "                                        </tr>",
            "                                        <tr>",
            "                                            <td style=\"font-weight: 700; color: #ff0000; font-size: 10.6667px; padding: 2px\">",
            "Son Ödeme Tarihi",
            "                                            </td>",
            "                                            <td style=\"font-weight: 700; text-align: right; font-size: 10.6667px; padding: 2px\">",
            m.ExpiryDateWithoutDayNameRepl,
            "                                            </td>",
            "                                        </tr>",
            "                                        <tr>",
            "                                            <td style=\"font-weight: 700; color: #ff0000; font-size: 10.6667px; padding: 2px\">",
            "Dönem Borcunuz",
            "                                            </td>",
            "                                            <td style=\"font-weight: 700; text-align: right; font-size: 10.6667px; padding: 2px\">",
            $"{m.ExtractDeptAmountInland} TL",
            "                                            </td>",
            "                                        </tr>",
            "                                        <tr>",
            "                                            <td style=\"font-weight: 700; color: #ff0000; font-size: 10.6667px; padding: 2px\">",
            "Asgari Ödeme Tutarı",
            "                                            </td>",
            "                                            <td style=\"font-weight: 700; text-align: right; font-size: 10.6667px; padding: 2px\">",
            $"{m.MinPaymentAmount} TL",
            "                                            </td>",
            "                                        </tr>",
            "                                        <tr>",
            "                                            <td style=\"font-weight: 700; color: #006f51; font-size: 10.6667px; padding: 2px\">",
            "Bekleyen Taksit Toplamı",
            "                                            </td>",
            "                                            <td style=\"font-weight: 700; text-align: right; font-size: 10.6667px; padding: 2px\">",
            $"{m.RemainInstallmentAmount} TL",
            "                                            </td>",
            "                                        </tr>",
            "                                    </tbody>",
            "                                </table>",
            "                            </div>",
            "                            <div style=\"margin-bottom: 20px\">",
            "                                <div style=\"font-family: 'Arial Black', Arial, sans-serif; font-size: 10.6667px; color: #006f51; margin-bottom: 5px\">",
            "Bir Sonraki Dönem Bilgileri",
            "                                </div>",
            "                                <hr style=\"border-top: 1px solid #000; margin-bottom: 5px\">",
            "                                <table style=\"width: 100%; border-collapse: collapse; font-family: Arial, Helvetica, sans-serif\">",
            "                                    <tbody>",
            "                                        <tr>",
            "                                            <td style=\"font-weight: 700; color: #006f51; font-size: 10.6667px; padding: 2px\">",
            "Hesap Kesim Tarihi",
            "                                            </td>",
            "                                            <td style=\"font-weight: 700; text-align: right; font-size: 10.6667px; padding: 2px\">",
            m.NextCreateDateWithDayNameRepl,
            "                                            </td>",
            "                                        </tr>",
            "                                        <tr>",
            "                                            <td style=\"font-weight: 700; color: #006f51; font-size: 10.6667px; padding: 2px\">",
            "Son Ödeme Tarihi",
            "                                            </td>",
            "                                            <td style=\"font-weight: 700; text-align: right; font-size: 10.6667px; padding: 2px\">",
            m.NextExpiryDateWithDayNameRepl,
            "                                            </td>",
            "                                        </tr>",
            "                                    </tbody>",
            "                                </table>",
            "                            </div>",
            "                            <div style=\"display: {ShowMilesSmiles}; margin-bottom: 20px\">",
            "                                <div style=\"font-family: 'Arial Black', Arial, sans-serif; font-size: 10.6667px; color: #006f51; margin-bottom: 5px\">",
            "Mil Bilgileri",
            "                                </div>",
            "                                <hr style=\"border-top: 1px solid #000; margin-bottom: 5px\">",
            "                                <table style=\"width: 100%; border-collapse: collapse; font-family: Arial, Helvetica, sans-serif\">",
            "                                    <tbody>",
            "                                        <tr>",
            "                                            <td style=\"font-weight: 700; color: #006f51; font-size: 10.6667px; padding: 2px\">",
            "TK Numarası",
            "                                            </td>",
            "                                            <td style=\"font-weight: 700; text-align: right; font-size: 10.6667px; padding: 2px\">",
            m.MainCardTKMemberId,
            "                                            </td>",
            "                                        </tr>",
            "                                        <tr>",
            "                                            <td style=\"font-weight: 700; color: #006f51; font-size: 10.6667px; padding: 2px\">",
            "Bu Dönem Kazanılan Mil",
            "                                            </td>",
            "                                            <td style=\"font-weight: 700; text-align: right; font-size: 10.6667px; padding: 2px\">",
            m.TotalMilScore,
            "                                            </td>",
            "                                        </tr>",
            "                                        <tr>",
            "                                            <td style=\"font-weight: 700; color: #006f51; font-size: 10.6667px; padding: 2px\">",
            "Alışveriş Mili",
            "                                            </td>",
            "                                            <td style=\"font-weight: 700; text-align: right; font-size: 10.6667px; padding: 2px\">",
            m.TotalShoppingMilScore,
            "                                            </td>",
            "                                        </tr>",
            "                                        <tr>",
            "                                            <td style=\"font-weight: 700; color: #006f51; font-size: 10.6667px; padding: 2px\">",
            "Kampanya ve Diğer Miller",
            "                                            </td>",
            "                                            <td style=\"font-weight: 700; text-align: right; font-size: 10.6667px; padding: 2px\">",
            m.TotalOtherMilScore,
            "                                            </td>",
            "                                        </tr>",
            "                                    </tbody>",
            "                                </table>",
            "                            </div>",
            "                        </div>",
            "                    </td>",
            "                </tr>",
            "            </table>",
            "        </div>",
            "    </body>",
            "</html>"
        };

        #endregion
    }

    class AnyContract
    {
        public string CardNumberHidden { get; set; }
        public string CreateDateWithoutDayNameRepl { get; set; }
        public string CreditLimitInland { get; set; }
        public string CustomerNameAndSurname { get; set; }
        public string CustomerNo { get; set; }
        public string DelayPenalty { get; set; }
        public string ExpiryDateWithoutDayName { get; set; }
        public string ExpiryDateWithoutDayNameRepl { get; set; }
        public string ExtractDeptAmountInland { get; set; }
        public string FullAddress { get; set; }
        public string GeneralContractProfitRate { get; set; }
        public string MainCardTKMemberId { get; set; }
        public string MilesSmilesCardSegment { get; set; }
        public string MinPaymentAmount { get; set; }
        public string MonthlyProfitRate { get; set; }
        public string NextCreateDateWithDayNameRepl { get; set; }
        public string NextExpiryDateWithDayNameRepl { get; set; }
        public string RemainCreditLimitInland { get; set; }
        public string RemainInstallmentAmount { get; set; }
        public string TotalMilScore { get; set; }
        public string TotalOtherMilScore { get; set; }
        public string TotalShoppingMilScore { get; set; }
    }

    class AnyRecord
    {
        public string Amount { get; set; }
        public string Date { get; set; }
    }

    class LineCollection : List<string>
    {
        public void Add(IEnumerable<LineCollection> lineCollections)
        {
            foreach (var collection in lineCollections)
            {
                AddRange(collection);
            }
        }
    }
}