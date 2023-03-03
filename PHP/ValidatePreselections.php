<?php

$clientId = $_POST["clientId"];
$projectId = $_POST["projectId"];
$selectedTiles = $_POST["preselectedTiles"];

// Retrieving the preselected tiles as an array
$preselectedTiles = array();
foreach ($selectedTiles as $key => $value) {
	$preselectedTiles[] = $value;
}

// Establish connection
try
{
	$bdd = new PDO('mysql:host=localhost;dbname=tpdemo;charset=utf8', 'root', '', array(PDO::ATTR_ERRMODE => PDO::ERRMODE_EXCEPTION, PDO::MYSQL_ATTR_LOCAL_INFILE => true));
}
catch(Exception $e)
{
	die('Error : ' . $e->getMessage());
}

$tableDeletion = "DROP TABLE IF EXISTS preselections;"; //!!! CAn't drop it if it's a common preselection list...but this is just a test
$result = $bdd->query($tableDeletion);
$result->closeCursor();

$tableCreation = "CREATE TABLE IF NOT EXISTS preselections ( client_id MEDIUMINT UNSIGNED NOT NULL DEFAULT '" . $clientId . "' , project_id MEDIUMINT UNSIGNED NOT NULL DEFAULT '" . $projectId . "', tile_id MEDIUMINT UNSIGNED NOT NULL, PRIMARY KEY (client_id, project_id, tile_id)) CHARACTER SET 'utf8' ENGINE=INNODB;";

$result = $bdd->query($tableCreation);
$result->closeCursor();


$tableInsertion = "INSERT INTO preselections (tile_id) SELECT id FROM `tptiles` WHERE `libelle`= '";

//$tableInsertion = "INSERT INTO preselections VALUES ";
$count = 0;
foreach ($selectedTiles as $key => $value) {
	if ($count > 0) {
		$tableInsertion = $tableInsertion . " OR `libelle`= '";
	}
	//$tableInsertion = $tableInsertion . "SELECT id FROM " + tilesTable + " WHERE libelle='" + tile + "';";
	$tableInsertion = $tableInsertion . $value . "'";
	$count += 1;
}
$tableInsertion = $tableInsertion . ";";

$result = $bdd->query($tableInsertion);
$result->closeCursor();

?>