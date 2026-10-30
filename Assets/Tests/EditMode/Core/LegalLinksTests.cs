using NUnit.Framework;

/// <summary>
/// URL of the privacy policy per language. The page has one section per language (#en, #es, #ca);
/// a language without its own section must still open the page, never a broken anchor.
/// </summary>
public class LegalLinksTests
{
    [TestCase("en", "#en")]
    [TestCase("es", "#es")]
    [TestCase("ca", "#ca")]
    [TestCase("es-ES", "#es")]
    [TestCase("CA", "#ca")]
    public void PrivacyPolicyUrl_GoesToTheSectionOfTheLanguage(string language, string anchor)
    {
        Assert.AreEqual(LegalLinks.PrivacyPolicy + anchor, LegalLinks.PrivacyPolicyUrl(language));
    }

    [TestCase(null)]
    [TestCase("")]
    [TestCase("fr")]
    public void PrivacyPolicyUrl_WithoutItsOwnSection_OpensThePageAtTheTop(string language)
    {
        Assert.AreEqual(LegalLinks.PrivacyPolicy, LegalLinks.PrivacyPolicyUrl(language));
    }

    [Test]
    public void PrivacyPolicy_IsAPublicHttpsPage()
    {
        StringAssert.StartsWith("https://", LegalLinks.PrivacyPolicy, "Google Play only accepts a public URL, and the browser opens it as it is");
        StringAssert.DoesNotContain("#", LegalLinks.PrivacyPolicy, "The anchor is added per language");
    }
}
