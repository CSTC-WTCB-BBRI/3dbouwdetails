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

$selectTiles = "SELECT c" . $clientId . "_p" . $projectId . "_surfaces.id_surface, room, validated, tile_category FROM c" . $clientId . "_p" . $projectId . "_surfaces LEFT JOIN c" . $clientId . "_p" . $projectId . "_validatedsurfaces ON c" . $clientId . "_p" . $projectId . "_surfaces.id_surface=c" . $clientId . "_p" . $projectId . "_validatedsurfaces.id_surface WHERE tile_category='A';";
$result = $bdd->query($selectTiles);

if ($result->errorCode() == 00000) 
{
  echo "Retrieve previous choice of tiles: OK\r\n";
} 
else 
{
  echo "Error while retrieving previous choice of tiles!\r\n";
}

if ($result->rowCount() > 0) {
	$rows = array();
    echo "RETURNS;\r\n";
}

// output data of each row
while($row = $result->fetch(PDO::FETCH_ASSOC)) {
	$rows[] = $row;
} 

//Close the query access
echo json_encode($rows);
$result->closeCursor();
?>