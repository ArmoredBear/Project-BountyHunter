
public partial class Scene_Data
{
    #region Variables

    public string path { get; set; }
    public string name { get; set; }
    public bool pause_allowed { get; set; }

    #endregion

    #region Constructor

    public Scene_Data(string _path, string _name, bool _pause_allowed)
    {
        this.path = _path;
        this.name = _name;
        this.pause_allowed = _pause_allowed;
    }

    #endregion
}
