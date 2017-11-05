using System.Collections.Generic;

public class PresenterService
{
  // stores code and group name
  public Dictionary<string, string> CodeStore = new Dictionary<string, string>();
  // connectionId -> groupname
  public Dictionary<string, string> Groups = new Dictionary<string, string>();
}