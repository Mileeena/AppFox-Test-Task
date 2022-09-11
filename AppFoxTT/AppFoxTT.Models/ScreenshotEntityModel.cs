namespace AppFoxTT.Models;

public class ScreenshotEntityModel
{
    int id { get; set; }
    string date { get; set; }
    public string screenshot { get; set; }

    public ScreenshotEntityModel(){}
    public ScreenshotEntityModel(int _id, string _date, string _screenshot)
    {
        id = _id;
        date = _date;
        screenshot = _screenshot;
    }
}