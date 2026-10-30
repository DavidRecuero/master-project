// Interface to open web pages outside the game (privacy policy, terms...), so the code that opens them can be tested
public interface IUrlOpener
{
    void Open(string url);
}
