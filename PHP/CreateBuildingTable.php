<?php

$clientId = $_POST["clientId"];
$projectId = $_POST["projectId"];

try
{
	$bdd = new PDO('mysql:host=localhost;dbname=tpdemo;charset=utf8', 'root', '', array(PDO::ATTR_ERRMODE => PDO::ERRMODE_EXCEPTION, PDO::MYSQL_ATTR_LOCAL_INFILE => true));
}
catch(Exception $e)
{
	die('Error : ' . $e->getMessage());
}

$deleteTable = "DROP TABLE IF EXISTS c" . $clientId . "_p" . $projectId . "_surfaces;";

$result = $bdd->query($deleteTable);
if ($result->errorCode() == 00000) 
{
  echo "Building table erase: OK\r\n";
} 
else 
{
  echo "Error during building table erasing!\r\n";
}

//Close the query access
$result->closeCursor();

$createCmd = "CREATE TABLE IF NOT EXISTS c" . $clientId . "_p" . $projectId . "_surfaces ( id_surface INT NOT NULL, area VARCHAR(20), level VARCHAR(20), surface_group SMALLINT, room VARCHAR(50), tile_category ENUM('', '0', 'A'), PRIMARY KEY (id_surface) ) CHARACTER SET 'utf8' ENGINE=INNODB;";

$result = $bdd->query($createCmd);
if ($result->errorCode() == 00000) 
{
  echo "Building table re-creation: OK\r\n";
} 
else 
{
  echo "Error during building table re-creation!\r\n";
}

//Close the query access
$result->closeCursor();

//Populate the table
$insertCmd = "INSERT INTO c" . $clientId . "_p" . $projectId . "_surfaces (id_surface, area, level, surface_group, room, tile_category) VALUES";

$ids = array();
$areas = array();
$levels = array();
$rooms = array();
$cats = array();

$count = 0;

foreach ($_POST["ID"] as $key => $value) {
	$ids[] = $value;
	$count += 1;
}
foreach ($_POST["Area"] as $key => $value) {
	$areas[] = $value;
}
foreach ($_POST["Level"] as $key => $value) {
	$levels[] = $value;
}
foreach ($_POST["Room"] as $key => $value) {
	$rooms[] = $value;
}
foreach ($_POST["TileCat"] as $key => $value) {
	$cats[] = $value;
}

$newCount = 0;
while ($newCount < $count) {
	if ($newCount > 0) {
		$insertCmd = $insertCmd . ", ";
	}
	$insertCmd = $insertCmd . "( '" . $ids[$newCount] . "', '" . $areas[$newCount] . "', '" . $levels[$newCount] .  "', NULL, '" . $rooms[$newCount] . "', '" . $cats[$newCount] . "')";
	$newCount += 1;
}

$insertCmd = $insertCmd . ";";

$result = $bdd->query($insertCmd);
if ($result->errorCode() == 00000) 
{
  echo "Building table filling: OK\r\n";
} 
else 
{
  echo "Error during building table filling!\r\n";
}

//Close the query access
$result->closeCursor();

?>