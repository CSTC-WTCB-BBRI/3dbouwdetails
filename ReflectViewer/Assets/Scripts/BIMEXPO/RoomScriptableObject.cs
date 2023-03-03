using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Reflect;
using System.Data;

[CreateAssetMenu(fileName = "RoomScriptableObject", menuName = "ScriptableObjects/RoomScriptableObject", order = 1)]
public class RoomScriptableObject : ScriptableObject
{
    [Header("BIMEXPO Custom Parameters")]
    public static string surface_type_parameter = "Comments";
    public static string room_parameter = "Mark";
    /// <summary>
    /// The list of all values the type of tilable surface can take.
    /// </summary>
    public static List<string> surface_type_values = new List<string>(new string[] { "A", "0"});
    /// <summary>
    /// The room in which the user currently is.
    /// </summary>
    public static string current_room;
    /// <summary>
    /// The surface currently being targeted/modified.
    /// </summary>
    public static GameObject current_surface;

    //private UnityAction m_MyFirstAction;
    public static DataSet roomsDataSet;

    private void OnEnable()
    {
        // Instantiate the DataSet variable.
        roomsDataSet = new DataSet();
    }

    /// <summary>
    /// Initializes the table of info for this room. Creates the table, insert rows with ids, comments, and areas.
    /// This method is triggered by the ListOfSurfacesSet event, which is raised in FindAllObjects, once the building is loaded.
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e">The name of the room and the list of surfaces in room, as GameObjects</param>
    public static void InitNewRoomTable(object sender, MyEventArgs e)
    {
        // Parse MyEventArgs
        string room_name = e.Args[0].ToString();
        List<GameObject> surfaces = (List<GameObject>)e.Args[1];

        // Create a new DataTable.
        DataTable table = new DataTable(room_name);
        // Declare variables for DataColumn and DataRow objects.
        DataColumn column;
        DataRow row;

        // Create new DataColumn, set DataType,
        // ColumnName and add to DataTable.
        column = new DataColumn();
        column.DataType = Type.GetType("System.Int32");
        column.ColumnName = "id";
        column.ReadOnly = true;
        column.Unique = true;
        // Add the Column to the DataColumnCollection.
        table.Columns.Add(column);

        // Create second column (Area).
        column = new DataColumn();
        column.DataType = Type.GetType("System.Single");
        column.ColumnName = "area";
        column.AutoIncrement = false;
        column.Caption = "Area";
        column.ReadOnly = true;
        column.Unique = false;
        // Add the column to the table.
        table.Columns.Add(column);

        // Create third column (Comments).
        column = new DataColumn();
        column.DataType = Type.GetType("System.Char");
        column.ColumnName = "tile_type";
        column.AutoIncrement = false;
        column.Caption = "Tile Type";
        column.ReadOnly = false;
        column.Unique = false;
        // Add the column to the table.
        table.Columns.Add(column);

        // Create fourth column (Type of tile).
        column = new DataColumn();
        column.DataType = Type.GetType("System.String");
        column.ColumnName = "tile";
        column.AutoIncrement = false;
        column.Caption = "Tile";
        column.ReadOnly = false;
        column.Unique = false;
        // Add the column to the table.
        table.Columns.Add(column);

        // Create fifth column (price of the surface).
        column = new DataColumn();
        column.DataType = Type.GetType("System.Single");
        column.ColumnName = "price";
        column.AutoIncrement = false;
        column.Caption = "Price";
        column.ReadOnly = false;
        column.Unique = false;
        // Add the column to the table.
        table.Columns.Add(column);

        // Create sixth column (list of screenshots associated to surface).
        column = new DataColumn();
        column.DataType = typeof(List<string>);
        column.ColumnName = "screenshots";
        column.AutoIncrement = false;
        column.Caption = "Screenshots";
        column.ReadOnly = false;
        column.Unique = false;
        // Add the column to the table.
        table.Columns.Add(column);

        // Create 7th column (has surface been modified during current session or not).
        column = new DataColumn();
        column.DataType = Type.GetType("System.Boolean");
        column.ColumnName = "modified";
        column.AutoIncrement = false;
        column.Caption = "Modified";
        column.ReadOnly = false;
        column.Unique = false;
        // Add the column to the table.
        table.Columns.Add(column);

        // Create 8th column (Comments).
        column = new DataColumn();
        column.DataType = Type.GetType("System.String");
        column.ColumnName = "comment";
        column.AutoIncrement = false;
        column.Caption = "Comments";
        column.ReadOnly = false;
        column.Unique = false;
        // Add the column to the table.
        table.Columns.Add(column);

        // Make the ID column the primary key column.
        DataColumn[] PrimaryKeyColumns = new DataColumn[1];
        PrimaryKeyColumns[0] = table.Columns["id"];
        table.PrimaryKey = PrimaryKeyColumns;

        // Add the new DataTable to the DataSet.
        roomsDataSet.Tables.Add(table);

        // Insert data to the DataTable
        foreach (GameObject go in surfaces)
        {
            // New row
            row = table.NewRow();

            // Add data
            Metadata meta = go.GetComponent<Metadata>();
            row["id"] = Int32.Parse(meta.GetParameter("Id"));
            if (meta.GetParameter("Comments") == "A")
            {
                row["tile_type"] = "A";
            }
            else if (meta.GetParameter("Comments") == "0")
            {
                row["tile_type"] = "0";
            }
            if (meta.GetParameter("BIMexpoArea") != null && meta.GetParameter("BIMexpoArea") != "")
            {
                row["area"] = float.Parse(BuildingInfo.areaRegex.Split(go.GetComponent<Metadata>().GetParameter("BIMexpoArea"))[0]);
            }
            row["tile"] = "-";
            row["price"] = 0.0;
            row["modified"] = false;
            row["comment"] = "";
            table.Rows.Add(row);
        }
    }

    /// <summary>
    /// This method is triggered by the NewTileChoice event, which is raised in NewTilesChoiceMenuScript, every time a tile is applied to a surface.
    /// Records the chosen tile for a given surface, along with its total price.
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="args">The name of the tile (string), and the name of the room (string)</param>
    public static void RecordTileChoice(object sender, MyEventArgs args)
    {
        // Parse MyEventArgs
        string tilename = args.Args[0].ToString();
        string room_name = args.Args[1].ToString();
        GameObject go;
        if (args.Args.Length == 3)
        {
            go = (GameObject)args.Args[2];
        }
        else
        {
            go = current_surface;
        }

        // Sets the tile in DataTable
        int id = Int32.Parse(go.GetComponent<Metadata>().GetParameter("Id"));
        string searchExpression = "id = " + id.ToString();
        DataTable table = roomsDataSet.Tables[room_name];
        DataRow row = table.Select(searchExpression)[0];
        row["tile"] = tilename;

        // Sets its price
        Web web = GameObject.Find("Root").GetComponent<Web>();
        double price_m2 = web.GetTilePriceFromLibelle(tilename);
        float area = (float)row["area"];
        double price = area * price_m2;
        row["price"] = price;

        // Mark it as modified during this session
        row["modified"] = true;
    }

    public static void RecordComment(string comment, string room, string id)
    {
        DataTable table = roomsDataSet.Tables[room];
        string filter = "id = '" + id + "'";
        if (table.Select(filter).Length != 1)
        {
            throw new Exception("DataSetAccessError");
        }
        else if (comment != "")
        {
            table.Select(filter)[0]["comment"] = comment;
        }
        return;
    }

    public static void RecordScreenshot(string room, string screenshot_path, Vector3 cam_position, Quaternion cam_rotation, string id)
    {
        // The cam position and rotation are passed to give the possibility of saving them as well
        // in order to be able to reconstruct the same view if needed later.
        // This is however not used at this stage.

        DataTable table = roomsDataSet.Tables[room];
        string filter = "id = '" + id + "'";
        if (table.Select(filter).Length != 1)
        {
            throw new Exception("DataSetAccessError");
        }
        else
        {
            if (DBNull.Value.Equals(table.Select(filter)[0]["screenshots"]))
            {
                table.Select(filter)[0]["screenshots"] = new List<string>(new string[] { screenshot_path });
            }
            else
            {
                List<string> sc_list = (List<string>)(IEnumerable<string>)table.Select(filter)[0]["screenshots"];
                if (!sc_list.Contains(screenshot_path))
                {
                    sc_list.Add(screenshot_path);
                }
                table.Select(filter)[0]["screenshots"] = sc_list;
            }
        }
        return;
    }

    public static float GetOverPrice(string room)
    {
        DataTable table = roomsDataSet.Tables[room];
        string filter = "tile_type = '0' And Not tile = '-'";
        DataRow[] foundRows = table.Select(filter);
        float price = 0.0f;
        foreach (DataRow row in foundRows)
        {
            price += (float)row["price"];
        }
        return price;
    }

}
