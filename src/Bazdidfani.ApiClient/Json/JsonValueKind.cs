namespace Bazdidfani.ApiClient.Json
{
    /// <summary>
    /// نوع یک مقدار JSON، مستقل از موتور سریالایز زیرین. ترتیب اعضا عمداً با
    /// System.Text.Json.JsonValueKind یکسان است تا تبدیل بین آن دو با یک cast ساده انجام شود.
    /// </summary>
    public enum JsonValueKind
    {
        /// <summary>مقدار یا پراپرتی وجود ندارد.</summary>
        Undefined = 0,

        /// <summary>شیء JSON.</summary>
        Object = 1,

        /// <summary>آرایهٔ JSON.</summary>
        Array = 2,

        /// <summary>مقدار رشته‌ای.</summary>
        String = 3,

        /// <summary>مقدار عددی.</summary>
        Number = 4,

        /// <summary>مقدار بولی <c>true</c>.</summary>
        True = 5,

        /// <summary>مقدار بولی <c>false</c>.</summary>
        False = 6,

        /// <summary>مقدار <c>null</c>.</summary>
        Null = 7,
    }
}
