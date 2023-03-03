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

$selectTiles = "SELECT * FROM c" . $clientId . "_p" . $projectId . "_validatedRooms ;";
$result = $bdd->query($selectTiles);

if ($result->errorCode() == 00000) 
{
  echo "Retrieve validated rooms: OK\r\n";
} 
else 
{
  echo "Error while retrieving the validated rooms!\r\n";
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