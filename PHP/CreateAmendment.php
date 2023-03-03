<?php

$clientId = $_POST["clientId"];
$projectId = $_POST["projectId"];
$session = $_POST["session"];

try
{
	$bdd = new PDO('mysql:host=localhost;dbname=tpdemo;charset=utf8', 'root', '', array(PDO::ATTR_ERRMODE => PDO::ERRMODE_EXCEPTION, PDO::MYSQL_ATTR_LOCAL_INFILE => true));
}
catch(Exception $e)
{
	die('Error : ' . $e->getMessage());
}

// Tables
$avenantTable = "c" . $clientId . "_p" . $projectId . "_avenant";
$surfacesTable = "c" . $clientId . "_p" . $projectId . "_surfaces";
$choicesTable = "c" . $clientId . "_p" . $projectId . "_choices";
$commentsTable = "c" . $clientId . "_p" . $projectId . "_comments";
$tilesTable = "tptiles";

// Delete old table
$tableDeletion = "DROP TABLE IF EXISTS " . $avenantTable . ";";
$result = $bdd->query($tableDeletion);
if ($result->errorCode() == 00000) 
{
  echo "Amendment old table deletion: OK\r\n";
} 
else 
{
  echo "Error while deleting the old amendment table!\r\n";
}
$result->closeCursor();

// Create the table
$tableCreation = "CREATE TABLE IF NOT EXISTS " . $avenantTable . " ( surface_id INT UNSIGNED NOT NULL, level VARCHAR(20), area VARCHAR(20), libelle VARCHAR(100), unit_price VARCHAR(20), total_price DOUBLE UNSIGNED, comment VARCHAR(200), session DATETIME, PRIMARY KEY (surface_id, session) ) CHARACTER SET 'utf8' ENGINE=INNODB;";
$result = $bdd->query($tableCreation);

if ($result->errorCode() == 00000) 
{
  echo "Amendment table creation: OK\r\n";
} 
else 
{
  echo "Error while creating the amendment table!\r\n";
}
$result->closeCursor();

//Make the join
$tableJoin = "REPLACE INTO " . $avenantTable . " SELECT " . $surfacesTable . ".id_surface, " . $surfacesTable . ".level, " . $surfacesTable . ".area, " . $tilesTable . ".libelle, "  . $tilesTable . ".prix_vente, " . $choicesTable . ".surface_price, " . $commentsTable . ".comment, " . $choicesTable . ".session";
$tableJoin = $tableJoin . " FROM " . $choicesTable;
$tableJoin = $tableJoin . " INNER JOIN " . $tilesTable . " ON " . $choicesTable . ".id_tile = " . $tilesTable . ".id";
$tableJoin = $tableJoin . " INNER JOIN " . $surfacesTable . " ON " . $choicesTable . ".id_surface = " . $surfacesTable . ".id_surface";
$tableJoin = $tableJoin . " LEFT JOIN " . $commentsTable . " ON " . $commentsTable . ".id_surface = " . $surfacesTable . ".id_surface";
$tableJoin = $tableJoin . " AND " . $commentsTable . ".session = " . $choicesTable . ".session;";

//echo $tableJoin;

$result = $bdd->query($tableJoin);

if ($result->errorCode() == 00000) 
{
  echo "Amendment table finalization: OK\r\n";
} 
else 
{
  echo "Error while finalizing the amendment table!\r\n";
}
$result->closeCursor();
?>