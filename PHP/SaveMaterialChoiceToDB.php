<?php

$session = $_POST["session"];
$surfaceId = $_POST["surfaceId"];
$tileName = $_POST["tileName"];
$clientId = $_POST["clientId"];
$projectId = $_POST["projectId"];
$tilePrice = $_POST["tilePrice"];
$areaString = $_POST["surfaceArea"];
$area = explode('m', $areaString)[0];
$totalPrice = floatval($area) * floatval($tilePrice);

try
{
	$bdd = new PDO('mysql:host=localhost;dbname=tpdemo;charset=utf8', 'root', '', array(PDO::ATTR_ERRMODE => PDO::ERRMODE_EXCEPTION, PDO::MYSQL_ATTR_LOCAL_INFILE => true));
}
catch(Exception $e)
{
	die('Error : ' . $e->getMessage());
}

$choiceInsertion = "REPLACE INTO c" . $clientId . "_p" . $projectId . "_choices (id_surface, id_tile, session) ";
$choiceInsertion = $choiceInsertion . "SELECT c" . $clientId . "_p" . $projectId . "_surfaces.id_surface, tptiles.id, '" . $session . "' ";
$choiceInsertion = $choiceInsertion . "FROM c" . $clientId . "_p" . $projectId . "_surfaces, tptiles ";
$choiceInsertion = $choiceInsertion . "WHERE libelle='" . $tileName . "' AND c" . $clientId . "_p" . $projectId . "_surfaces.id_surface='" . $surfaceId . "';";

$result = $bdd->query($choiceInsertion);

if ($result->errorCode() == 00000) 
{
  echo "User's choices table update: OK\r\n";
} 
else 
{
  echo "Error while updating the user's choices table!\r\n";
}

//Close the query access
$result->closeCursor();

// Now the price
$priceCmd = "UPDATE c" . $clientId . "_p" . $projectId . "_choices SET surface_price='" . $totalPrice . "' WHERE id_surface='" . $surfaceId . "';";
$result = $bdd->query($priceCmd);
if ($result->errorCode() == 00000) 
{
  echo "User's choices table update of price: OK\r\n";
} 
else 
{
  echo "Error while updating the price of the user's choices table!\r\n";
}
//Close the query access
$result->closeCursor();

?>